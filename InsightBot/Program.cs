using Insight_bott;
using Insight_bott.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

// создаем соединение с базой в классе DbHelper чтобы потом можно было из разных местах программы с базой работать
DbHelper db_new = new DbHelper();

// добавляем в список для уведомлений админа уведомление в консоль
AnswersMethods.RegisterNotifier(Console.WriteLine);
// добавляем в список для уведомлений админа уведомление в личные сообщения телеграма
AnswersMethods.RegisterNotifier(SendMessageToAdminInTelegram);


// создаем сервис логгирования
ServiceProvider.CreateServiceProvider();

//эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
DotNetEnv.Env.TraversePath().Load();
var telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

// создаем клиент телеграмма в классе TelegramBotHelper через который можно будет в любом участке программа слать сообщения
TelegramBotHelper telegramBotHelperClient = new TelegramBotHelper(telegramBotApiKey);


// Уведомляем админа о запуске бота
AnswersMethods.AdminNotifier("Бот запущен");

// запускаем шедулер для ежедневных уведомлений
Insight_bott.Jobs.Sheduler sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start();

TelegramBotHelper.Client.StartReceiving(Update, Error);
static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
{
    throw new NotImplementedException();
}

async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
{
    var message = update.Message;

    // если есть текст сообщения
    if (message!=null && message.Text != null)
    {
        // логируем запрос
        ServiceProvider.Logger.Write($"Юзер {message.Chat.Username} c id {message.Chat.Id} сделал запрос: {message.Text}.");

        var listOfCommands = new List<string>() { "/start", "/get_insight", "/add_new_insight", "/help", "/random_insight" };
        long currentUserTgId = message.Chat.Id;

        if (listOfCommands.Contains(message.Text))
        {
            switch (message.Text.ToLower())
            {
                case "/start":
                    AnswersMethods.Start(botClient, message, currentUserTgId, token);
                    break;
                case "/get_insight":
                    try
                    {
                        AnswersMethods.GetInsight(currentUserTgId, out string textOfInsight, out int idOfUserInsightInDb);
                        AnswersMethods.SendInsight(textOfInsight, idOfUserInsightInDb, currentUserTgId);
                    }
                    catch (Exception)
                    {
                        AnswersMethods.SendMessage(
                            currentUserTgId, 
                            "Список инсайтов пуст. Добавьте новый инсайт /add_new_insight",
                            out int idOfMessage);
                    }
                    break;
                case "/random_insight":
                    // тут можно переписать чтобы сразу корректно подтягивались данные
                    var UserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
                    if (UserFromDb is null)
                    {
                        AnswersMethods.Start(botClient, message, currentUserTgId, token);
                        UserFromDb = DbHelper.db.Users.Find(currentUserTgId);
                    }
                    DbHelper.db.Entry(UserFromDb).Collection(c => c.Insights).Load();
                    
                    UserFromDb.GetRandomInsight(out string textOfRandomInsight, out int idRandomInsight);
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
                        "Для добавления мысли нажмите /add_new_insight", out int idOfMessage);
                    break;                   
                }

            }
        }
        // просто пришел какой-то текст
        else
        {
            //юзер который отправил текст
            var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId);
            // если у пользователь ранее отправлял команду /add_new_insight
            if (currentUserFromDb.WantToAddAnInsight)
            {
                // сохраняем новый инсайт в список инсайтов пользователя
                currentUserFromDb.AddNewInsight(message.Text);
                var answer = await DbHelper.db.SaveChangesAsync(); // сохранение 
                AnswersMethods.SendMessage(message.Chat.Id, "Инсайт сохранен", out int messageId);
                
                // id нового инсайта в db
                var idOfNewInsight = currentUserFromDb.Insights.Last().Id;
                // тут же отрпавляем этот инсайт, чтобы была возможность его удалить
                AnswersMethods.SendInsight(message.Text, idOfNewInsight, currentUserTgId);
            }
        }
    }
    // если событие является колбэком
    else if (update.CallbackQuery != null)
    {
        var callbackQueryId = update.CallbackQuery.Id;
        
        // получаем id инсайта и информацию о кнопке колбэка
        string[] dataFromButton = update.CallbackQuery.Data.Split(",");
        int idOfInsight = Convert.ToInt32(dataFromButton[0]);
        string textOfButton = dataFromButton[1];
        int messageId = Convert.ToInt32(dataFromButton[2]);
        
        // получаем userTelegramId пользователя
        var userTelegramId = update.CallbackQuery.From.Id;
        
        // получаем юзера и его список инсайтов из БД
        var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
        DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();

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
                setRepeat(1, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
                break;
            case"Повторить через день":
                setRepeat(2, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
                break;
            case "Повторить через неделю":
                setRepeat(7, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
                break;
            case "Регулярное повторение":
                //setRepeat(7, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
                break;
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
                AnswersMethods.CreateBaseInlineButtons(idOfInsight, out InlineKeyboardMarkup inlineKeyboard, messageId);
                TelegramBotHelper.Client.EditMessageReplyMarkupAsync(userTelegramId, messageId, inlineKeyboard);
                break;                
            }

        }


    }
    // какой-то непредусмотренный тип события
    else
    {
        Console.WriteLine($"Пришло какое-то непредвиденное событие типа {message.Type}."); // тут бы логирование добавить
        AnswersMethods.AdminNotifier(
            $"Юзер {message.Chat.Username} c id {message.Chat.Id} прислал непредвиденное событие типа {message.Type}");
    }
}

Console.ReadLine();

//МЕТОДЫ ДЛЯ РАБОТЫ

// уведомления в личку админу
// отправляем админу сообщение о том что бот запущен
static void SendMessageToAdminInTelegram(string messageToAdmin)
{
    var adminId = Convert.ToInt64(Environment.GetEnvironmentVariable("ADMIN_ID"));
    AnswersMethods.SendMessage(adminId, messageToAdmin, out int messageId);
}


// сохраняет дату повторения инсайта 
static async void setRepeat(
    int inHowManyDaysToRepeat,
    ITelegramBotClient botClient, 
    Insight_bott.User? currentUserFromDb,
    int idOfInsight,
    string callbackQueryId)
{
    bool isFound = false;
    // ищем инсайт по которому нужно сохранить дату повторения
    foreach (Insight insight in currentUserFromDb.Insights)
    {
        if (insight.Id == idOfInsight)
        {
            isFound = true;
            insight.CreateRepeat(inHowManyDaysToRepeat);
            await DbHelper.db.SaveChangesAsync();
            string dateOfRepeting = insight.WhenToRepeat.Value.ToShortDateString();
            await botClient.AnswerCallbackQueryAsync(callbackQueryId, $"Повторю {dateOfRepeting}");
            break;
        }
    }
    // если не нашли инсайт по которому пришел запрос
    if (isFound != true)
    {
        await botClient.AnswerCallbackQueryAsync(callbackQueryId, "Инсайт не найден");
    }
}