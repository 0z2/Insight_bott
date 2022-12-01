using Insight_bott;
using Telegram.Bot;
using Telegram.Bot.Types;
using Insight_bott.Logging;
using Microsoft.Extensions.DependencyInjection;

// создаем соединение с базой в классе DbHelper чтобы потом можно было из разных местах программы с базой работать
DbHelper db_new = new DbHelper();

// создаем сервис логирования
IServiceCollection services = new ServiceCollection()
    .AddSingleton<ILogService, SimpleLogService>();
using ServiceProvider serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetService<ILogService>();


//эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
DotNetEnv.Env.TraversePath().Load();
var telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

// создаем клиент телеграмма в классе TelegramBotHelper через который можно будет в любом участке программа слать сообщения
TelegramBotHelper telegramBotHelperClient = new TelegramBotHelper(telegramBotApiKey);


// отправляем админу сообщение о том что бот запущен
var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");
Message message = await TelegramBotHelper.Client.SendTextMessageAsync(
    chatId: adminId,
    text: "Бот запущен!");

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
    // если событие является текстом
    if (message != null)
    {
        // логируем запрос
        logger.Write($"Юзер {message.Chat.Username} c id {message.Chat.Id} сделал запрос: {message.Text}.");
        
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
                        botClient.SendTextMessageAsync(
                            chatId: currentUserTgId,
                            text: $"Список инсайтов пуст. Добавьте новый инсайт /add_new_insight");
                    }
                    break;
                case "/random_insight":
                    // тут можно переписать чтобы сразу корректно подтягивались данные
                    var UserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
                    DbHelper.db.Entry(UserFromDb).Collection(c => c.Insights).Load();
                    
                    UserFromDb.GetRandomInsight(out string textOfRandomInsight, out int idRandomInsight);
                    AnswersMethods.SendInsight(textOfRandomInsight, idRandomInsight, currentUserTgId);
                    break;
                case "/add_new_insight":
                    var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
                    currentUserFromDb.WantToAddAnInsight = true;
                    await DbHelper.db.SaveChangesAsync(token); // сохранение 
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите текст инсайта");
                    break;
                case "/help":
                    await botClient.SendTextMessageAsync(
                        message.Chat.Id, "В этом боте вы можете сохранять значимые для себя мысли. " +
                                         "Каждое утро бот будет присылать по одной из них.\n" +
                                         "Для добавления мысли нажмите /add_new_insight");
                    break;
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
                await botClient.SendTextMessageAsync(message.Chat.Id, "Инсайт сохранен");
                
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
        
        // получаем userTelegramId пользователя
        var userTelegramId = update.CallbackQuery.From.Id;
        
        // получаем юзера и его список инсайтов из БД
        var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
        DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
        
        if (textOfButton == "Удалить")
        {
            currentUserFromDb.DeleteInsight(idOfInsight, out bool isDelited);
            if (isDelited)
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт удален");
                await DbHelper.db.SaveChangesAsync();
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт уже был удален ранее");
            }
        }
        else if (textOfButton == "Повторить завтра")
        {   
            setRepeat(1, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
        }
        else if (textOfButton == "Повторить через день")
        {
            setRepeat(2, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
        }
        else if (textOfButton == "Повторить через неделю")
        {
            setRepeat(7, botClient, currentUserFromDb, idOfInsight, callbackQueryId);
        }

    }
    else
    {
        Console.WriteLine("Пришло какое-то непредвиденное событие."); // тут бы добавить админу уведомление
    }
}

Console.ReadLine();

//МЕТОДЫ ДЛЯ РАБОТЫ

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