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

TelegramBotHelper.Client.StartReceiving(Update, Error);
static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
{
    throw new NotImplementedException();
}

async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
{
    var message = update.Message;

    if (message != null)
    {
        // вариант логгирования просто через интерфейс. Здесь каждый раз независимый объет создается
        // //Console.WriteLine($" {message.Chat.Id} сделал запрос {message.Text}.");
        // var logger = new Logger(new SimpleLogService());
        // //logger.Log($" {message.Chat.Id} сделал запрос {message.Text}.");
        //  
        // logger = new Logger(new GreenLogService());
        // logger.Log($" {message.Chat.Id} сделал запрос {message.Text}.");
        
        
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
        else
        {
            //юзер который отправил инсайт
            var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId);
            if (currentUserFromDb.WantToAddAnInsight)
            {
                // подтягиваем контекст инсайтов по пользователю
                //DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
                
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
        string[] dataFromButton = update.CallbackQuery.Data.Split(",");
        string idOfInsight = dataFromButton[0];
        string textOfButton = dataFromButton[1];
        if (textOfButton == "Удалить")
        {
            // получаем userTelegramId пользователя, запросившего удаление
            var userTelegramId = update.CallbackQuery.From.Id;
            // извлекаем номер инсайта, который необходимо удалить
            var idInsightForDeleting = Convert.ToInt32(idOfInsight);
        
            //юзер который отправил инсайт
            var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
            // добавляем в контекст DB инсайты пользователя
            DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
        
            currentUserFromDb.DeleteInsight(idInsightForDeleting, out bool isDelited);

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
            // ЕСТЬ ПОВТОРЕНИЕ ЭТОГО КОДА, ВЫНЕСТИ В ОТДЕЛЬНЫЙ МЕТОД
            // получаем userTelegramId пользователя, запросившего удаление
            var userTelegramId = update.CallbackQuery.From.Id;
            // извлекаем номер инсайта, который необходимо удалить
            var idInsightForRepiting= Convert.ToInt32(idOfInsight);
            //юзер который отправил инсайт
            var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
            // добавляем в контекст DB инсайты пользователя
            DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
            
            bool wasFound = false;
            foreach (Insight insight in currentUserFromDb.Insights)
            {
                
                if (insight.Id == idInsightForRepiting)
                {
                    insight.CreateRepeat(1);
                    await DbHelper.db.SaveChangesAsync();
                    wasFound = true;
                    
                    
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Повторю завтра");

                    break;
                }
            }
            if (wasFound != true)
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт не найден");
            }
            
        }
        else if (textOfButton == "Повторить через день")
        {
            // ЕСТЬ ПОВТОРЕНИЕ ЭТОГО КОДА, ВЫНЕСТИ В ОТДЕЛЬНЫЙ МЕТОД
            // получаем userTelegramId пользователя, запросившего удаление
            var userTelegramId = update.CallbackQuery.From.Id;
            // извлекаем номер инсайта, который необходимо удалить
            var idInsightForRepiting= Convert.ToInt32(idOfInsight);
            //юзер который отправил инсайт
            var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
            // добавляем в контекст DB инсайты пользователя
            DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
            
            bool wasFound = false;
            foreach (Insight insight in currentUserFromDb.Insights)
            {
                
                if (insight.Id == idInsightForRepiting)
                {
                    insight.CreateRepeat(2);
                    await DbHelper.db.SaveChangesAsync();
                    wasFound = true;
                    
                    
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Повторю через день");

                    break;
                }
            }
            if (wasFound != true)
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт не найден");
            }
        }
        else if (textOfButton == "Повторить через неделю")
        {
            // ЕСТЬ ПОВТОРЕНИЕ ЭТОГО КОДА, ВЫНЕСТИ В ОТДЕЛЬНЫЙ МЕТОД
            // получаем userTelegramId пользователя, запросившего удаление
            var userTelegramId = update.CallbackQuery.From.Id;
            // извлекаем номер инсайта, который необходимо удалить
            var idInsightForRepiting= Convert.ToInt32(idOfInsight);
            //юзер который отправил инсайт
            var currentUserFromDb = DbHelper.db.Users.Find(userTelegramId);
            // добавляем в контекст DB инсайты пользователя
            DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
            
            bool wasFound = false;
            foreach (Insight insight in currentUserFromDb.Insights)
            {
                
                if (insight.Id == idInsightForRepiting)
                {
                    insight.CreateRepeat(7);
                    await DbHelper.db.SaveChangesAsync();
                    wasFound = true;
                    
                    
                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Повторю через неделю");

                    break;
                }
            }
            if (wasFound != true)
            {
                await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "Инсайт не найден");
            }
        }

    }
    else
    {
        Console.WriteLine("Пришло какое-то непредвиденное событие."); // тут бы добавить админу уведомление
    }
}

// запускаем шедулер для ежедневных уведомлений
Insight_bott.Jobs.Sheduler sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start();

Console.ReadLine();

