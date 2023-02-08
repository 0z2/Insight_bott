using Insight_bott;
using Insight_bott.Logging;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;


// создаем соединение с базой в классе DbHelper чтобы потом можно было из разных мест программы с базой работать
var db_new = new DbHelper();

// добавляем в список для уведомлений админа уведомление в консоль и телеграм
AnswersMethods.RegisterNotifier(Console.WriteLine);
AnswersMethods.RegisterNotifier(SendMessageToAdminInTelegram);

// создаем сервис логгирования
ServiceProvider.CreateServiceProvider();

//эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
DotNetEnv.Env.TraversePath().Load();
var telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

// создаем клиент телеграмма в классе TelegramBotHelper через который можно будет в любом участке программа слать сообщения
var telegramBotHelperClient = new TelegramBotHelper(telegramBotApiKey);

AnswersMethods.AdminNotifier("Бот запущен");

// запускаем шедулер для ежедневных уведомлений
var sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start();

TelegramBotHelper.Client.StartReceiving(Update, Error);

static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
{
    throw new NotImplementedException();
}

async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
{
    var message = update.Message;


    if (message != null && message.Text != null)
    {
        ServiceProvider.Logger.Write(
            $"Юзер {message.Chat.Username} c id {message.Chat.Id} сделал запрос: {message.Text}.");

        var currentUserTgId = message.Chat.Id;
        var UserFromDb = DbHelper.db.Users.Find(currentUserTgId);

        var listOfCommands = new List<string>()
            { "/start", "/get_insight", "/add_new_insight", "/help", "/random_insight" };

        if (UserFromDb is null)
        {
            AnswersMethods.Start(botClient, message, currentUserTgId, token);
            UserFromDb = DbHelper.db.Users.Find(currentUserTgId);
        }

        if (UserFromDb.isAsign == false)
        {
            AnswersMethods.AdminNotifier(
                $"Юзер {message.Chat.Username} c id {message.Chat.Id} заблокировал сообщения");
            ServiceProvider.Logger.Write("Юзер {message.Chat.Username} c id {message.Chat.Id} заблокировал сообщения");
        }
        else if (listOfCommands.Contains(message.Text))
        {
            switch (message.Text.ToLower())
            {
                case "/get_insight":
                    try
                    {
                        AnswersMethods.GetInsight(currentUserTgId, out var textOfInsight, out var idOfUserInsightInDb);
                        AnswersMethods.SendInsight(textOfInsight, idOfUserInsightInDb, currentUserTgId);
                    }
                    catch (Exception)
                    {
                        AnswersMethods.SendMessage(
                            currentUserTgId,
                            "Список инсайтов пуст. Добавьте новый инсайт /add_new_insight",
                            out var idOfMessage);
                    }

                    break;
                case "/random_insight":
                    UserFromDb =
                        DbHelper.db.Users
                            .Include(user => user.Insights)
                            .FirstOrDefault(user => user.TelegramId == currentUserTgId);

                    UserFromDb.GetRandomInsight(out var textOfRandomInsight, out var idRandomInsight);
                    AnswersMethods.SendInsight(textOfRandomInsight, idRandomInsight, currentUserTgId);
                    break;
                case "/add_new_insight":
                {
                    var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
                    currentUserFromDb.WantToAddAnInsight = true;
                    await DbHelper.db.SaveChangesAsync(token); // сохранение 
                    AnswersMethods.SendMessage(message.Chat.Id, "Введите текст инсайта", out var idOfMessage);
                    break;
                }
                case "/help":
                {
                    AnswersMethods.SendMessage(message.Chat.Id,
                        "В этом боте вы можете сохранять значимые для себя мысли. " +
                        "Каждое утро бот будет присылать по одной из них.\n" +
                        "Для добавления мысли нажмите /add_new_insight", out var idOfMessage);
                    break;
                }
            }
        }
        // текст не из списка команд
        else
        {
            //юзер который отправил текст
            var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId);

            if (currentUserFromDb is null)
            {
                AnswersMethods.Start(botClient, message, currentUserTgId, token);
                currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId);
            }

            // если пользователь ранее отправлял команду /add_new_insight
            if (currentUserFromDb.WantToAddAnInsight)
            {
                // сохраняем новый инсайт в список инсайтов пользователя
                currentUserFromDb.AddNewInsight(message.Text);
                var answer = await DbHelper.db.SaveChangesAsync(); // сохранение 
                AnswersMethods.SendMessage(message.Chat.Id, "Инсайт сохранен", out var messageId);

                // id нового инсайта в db
                var idOfNewInsight = currentUserFromDb.Insights.Last().Id;
                // тут же отрпавляем этот инсайт, с инлайн кнопками для уадления/настройки повторений
                AnswersMethods.SendInsight(message.Text, idOfNewInsight, currentUserTgId);
            }
        }
    }
    else if (update.CallbackQuery != null)
    {
        var callbackQueryId = update.CallbackQuery.Id;

        // получаем id инсайта и информацию о кнопке колбэка
        var dataFromButton = update.CallbackQuery.Data.Split(",");
        var idOfInsight = Convert.ToInt32(dataFromButton[0]);
        var textOfButton = dataFromButton[1];
        var messageId = Convert.ToInt32(dataFromButton[2]);

        // получаем userTelegramId пользователя
        var userTelegramId = update.CallbackQuery.From.Id;

        // получаем юзера и его список инсайтов из БД
        var currentUserFromDb =
            DbHelper.db.Users
                .Include(user => user.Insights)
                .FirstOrDefault(user => user.TelegramId == userTelegramId);

        switch (textOfButton)
        {
            case "Удалить":
                currentUserFromDb.DeleteInsight(idOfInsight, out var isDelited);
                if (isDelited)
                {
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт удален");
                    await DbHelper.db.SaveChangesAsync();
                }
                else
                {
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт уже был удален ранее");
                }

                break;
            case "Повторить завтра":
            {
                var information = new InformationForSingleReptition()
                {
                    TextOfMessage = "Повторить завтра",
                    InHowManyDaysToRepeat = 1
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetSingleRepeat,
                    information);
                break;
            }

            case "Повторить через день":
            {
                var information = new InformationForSingleReptition()
                {
                    TextOfMessage = "Повторю послезавтра",
                    InHowManyDaysToRepeat = 2
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetSingleRepeat,
                    information);
            }

                break;
            case "Повторить через неделю":
            {
                var information = new InformationForSingleReptition()
                {
                    TextOfMessage = "Повторю через неделю",
                    InHowManyDaysToRepeat = 7
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetSingleRepeat,
                    information);
                break;
            }

            case "Повторять ежедневно":
            {
                var information = new InformationForRegularReptition()
                {
                    TextOfMessage = "Ежедневное повторение",
                    HowOftenInDaysRepeat = 1
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetRegularRepeatition,
                    information);
                
                // обновляем кнопки
                AnswersMethods.CreateRegularReptitionInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;
            }

            case "Повторять через день":
            {
                var information = new InformationForRegularReptition()
                {
                    TextOfMessage = "Повторение через день",
                    HowOftenInDaysRepeat = 2
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetRegularRepeatition,
                    information);
                
                // обновляем кнопки
                AnswersMethods.CreateRegularReptitionInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;
            }
            case "Повторять еженедельно":
            {
                var information = new InformationForRegularReptition()
                {
                    TextOfMessage = "Еженедельное повторение",
                    HowOftenInDaysRepeat = 7
                };
                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    SetRegularRepeatition,
                    information);
                
                // обновляем кнопки
                AnswersMethods.CreateRegularReptitionInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;
            }
            case "Регулярное повторение":
            {
                // меняем базовые кнопки на кнопки регулярного повторения
                AnswersMethods.CreateRegularReptitionInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;
            }
            case "Разовое повторение":
            {
                // меняем базовые кнопки на кнопки разового повторения
                AnswersMethods.CreateSingleReptitionInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);

                break;
            }
            case "Назад":
            {
                // меняем кнопки повторений на базовые кнопки
                AnswersMethods.CreateBaseInlineButtons(idOfInsight, out var inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;
            }
            case "Отключить повторения":
            {
                var clearRepetition = (Insight insight, IInformationForFunctions information) =>
                {
                    insight.WhenToRepeat = null;
                    insight.HowOftenRepeatInDays = null;
                };
                var information = new InformationForClearReptition()
                {
                    TextOfMessage = "Повторения отключены"
                };

                changeRepetition(
                    botClient,
                    currentUserFromDb,
                    idOfInsight,
                    callbackQueryId,
                    clearRepetition,
                    information);
                break;
            }
        }
    }
    // какой-то непредусмотренный тип события
    else
    {
        if (message is not null)
        {
            AnswersMethods.AdminNotifier(
                $"Юзер {message.Chat.Username} c id {message.Chat.Id} прислал непредвиденное событие типа {message.Type}");
            ServiceProvider.Logger.Write(
                $"Юзер {message.Chat.Username} c id {message.Chat.Id} прислал непредвиденное событие типа {message.Type}");
        }
        else
        {
            AnswersMethods.AdminNotifier(
                $"Неизвестный юзер прислал непредвиденное событие непонятного типа");
            ServiceProvider.Logger.Write(
                $"Неизвестный юзер прислал непредвиденное событие непонятного типа");            
        }
    }

}
Console.ReadLine();

//МЕТОДЫ ДЛЯ РАБОТЫ

// уведомления в личку админу
// отправляем админу сообщение о том что бот запущен
static void SendMessageToAdminInTelegram(string messageToAdmin)
{
    var adminId = Convert.ToInt64(Environment.GetEnvironmentVariable("ADMIN_ID"));
    AnswersMethods.SendMessage(adminId, messageToAdmin, out var messageId);
}

// сохраняет дату разового повторения инсайта 
static void SetSingleRepeat(Insight insight, IInformationForFunctions information)
{
    var info = information as InformationForSingleReptition;
    insight.CreateSingleRepeatition(info.InHowManyDaysToRepeat);
}

// сохраняет информацию о регулярном повторении
static void SetRegularRepeatition(Insight insight, IInformationForFunctions information)
{
    var info = information as InformationForRegularReptition;
    insight.CreateRegularRepeatition(info.HowOftenInDaysRepeat);
}

// сохраняет дату разового повторения инсайта 
static async void changeRepetition(
    ITelegramBotClient botClient,
    Insight_bott.User? currentUserFromDb,
    int idOfInsight,
    string callbackQueryId,
    Action<Insight, IInformationForFunctions> changeRepitionFunc,
    IInformationForFunctions information
)
{
    // ищем инсайт по которому нужно сохранить дату повторения
    var insight = currentUserFromDb.Insights.Find(insight => insight.Id == idOfInsight);
    if (insight is not null)
    {
        changeRepitionFunc(insight, information);
        await DbHelper.db.SaveChangesAsync();
        await botClient.AnswerCallbackQueryAsync(callbackQueryId, information.TextOfMessage);        
    }
    // если не нашли инсайт по которому пришел запрос
    else await botClient.AnswerCallbackQueryAsync(callbackQueryId, "Инсайт не найден");
}