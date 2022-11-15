using Quartz;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data.Entity;
using System.Diagnostics;
using System.Threading.Tasks;
using EntityState = Microsoft.EntityFrameworkCore.EntityState;

namespace Insight_bott.Jobs
{
    public class Sender : IJob
    {

        TelegramBotClient Client;

        public Sender()
        {
            // эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
            DotNetEnv.Env.TraversePath().Load();
            var telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
            
            Client = new TelegramBotClient(telegramBotApiKey);
            Client.StartReceiving(Update, Error);
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            // эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
            DotNetEnv.Env.TraversePath().Load();
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");
            
            Message message = await Client.SendTextMessageAsync(
            chatId: adminId,
            text: "Бот запущен!");
        }
        static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }

        async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            var currentUserTgId = message.Chat.Id;
            var listOfCommands = new List<string>() { "/start", "/get_insight", "/add_new_insight"};
            
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
                            await using (ApplicationContext db = new ApplicationContext())
                            {
                                var currentUserFromDb = db.Users.Find(currentUserTgId); //юзер который запросил мысль
                                currentUserFromDb.WantToAddAnInsight = true;
                                await db.SaveChangesAsync(token); // сохранение 
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Введите текст инсайта");
                            }

                            break;
                    }
                }
                else
                {
                    await using (ApplicationContext db = new ApplicationContext())
                    {
                        var currentUserFromDb = db.Users.Find(currentUserTgId);; //юзер который запросил мысль
                
                        if (currentUserFromDb.WantToAddAnInsight)
                        {
                            currentUserFromDb.AddNewInsight(message.Text);
                            await db.SaveChangesAsync(); // сохранение 
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Инсайт сохранен");
                    }
                }
            }
        }
    }
}

    