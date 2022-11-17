using Insight_bott;
using Telegram.Bot;
using Telegram.Bot.Types;


DbHelper db_new = new DbHelper();

TelegramBotClient Client;


// эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
DotNetEnv.Env.TraversePath().Load();
var telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");

Client = new TelegramBotClient(telegramBotApiKey);
Client.StartReceiving(Update, Error);


// эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
DotNetEnv.Env.TraversePath().Load();
var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");

Message message = await Client.SendTextMessageAsync(
    chatId: adminId,
    text: "Бот запущен!");

TelegramBotClient GetTelegramBotClient()
{
    return Client;
}

static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
{
    throw new NotImplementedException();
}

async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
{
    var message = update.Message;
    var currentUserTgId = message.Chat.Id;
    var listOfCommands = new List<string>() { "/start", "/get_insight", "/add_new_insight" };

    if (message.Text != null)
    {
        Console.WriteLine($" {message.Chat.Id} сделал запрос.");

        if (listOfCommands.Contains(message.Text))
        {
            switch (message.Text.ToLower())
            {
                case "/start":
                    AnswersMethods.Start(botClient, message, currentUserTgId, token);
                    break;
                case "/get_insight":
                    AnswersMethods.GetInsight(botClient, message, currentUserTgId, token);
                    break;
                case "/add_new_insight":

                    var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
                    currentUserFromDb.WantToAddAnInsight = true;
                    await DbHelper.db.SaveChangesAsync(token); // сохранение 
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите текст инсайта");


                    break;
            }
        }
        else
        {
            // await using (ApplicationContext db = new ApplicationContext())
            // {
                var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId);
                ; //юзер который запросил мысль

                if (currentUserFromDb.WantToAddAnInsight)
                {
                    currentUserFromDb.AddNewInsight(message.Text);
                    await DbHelper.db.SaveChangesAsync(); // сохранение 
                }

                await botClient.SendTextMessageAsync(message.Chat.Id, "Инсайт сохранен");
            // }
        }
    }
}


// запускаем шедулер для ежедневных уведомлений
Insight_bott.Jobs.Sheduler sheduler = new Insight_bott.Jobs.Sheduler();
sheduler.Start(Client);

Console.ReadLine();

