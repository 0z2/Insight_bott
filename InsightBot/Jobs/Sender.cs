using Quartz;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data.Entity;
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
            Message message = await Client.SendTextMessageAsync(
            chatId: 985485455,
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
            var listOfCommands = new List<string>() { "/start", "здорова", "/get_thought", "/add_new_insight"};
            
            if (message.Text != null)
            {
                Console.WriteLine($" {message.Chat.Id} сделал запрос.");

                if (listOfCommands.Contains(message.Text))
                {
                    if (message.Text.ToLower() == "/start")
                    {
                        AnswersMethods.Start(botClient, message, currentUserTgId, token);
                    }
                
                    else if (message.Text.ToLower().Contains("здорова"))
                    {
                        AnswersMethods.Zdorova(botClient, message);
                    }
                
                    else if (message.Text.ToLower() == "/get_thought")
                    {
                        AnswersMethods.GetThought(Client, botClient, message, currentUserTgId, token);
                    }
                    
                    else if (message.Text.ToLower() == "/add_new_insight")
                    {
                        await using (ApplicationContext db = new ApplicationContext())
                        {
                            User currentUserFromDb = Users.GetUser(currentUserTgId, token, db); //юзер который запросил мысль
                            currentUserFromDb.WantToAddAnInsight = true;
                            await db.SaveChangesAsync(token); // сохранение 
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Введите текст инсайта");
                        }
                    }
                }
                else
                {
                    await using (ApplicationContext db = new ApplicationContext())
                    {
                        User currentUserFromDb = Users.GetUser(currentUserTgId, token, db); //юзер который запросил мысль
                        
                        if (currentUserFromDb.WantToAddAnInsight)
                        {
                            currentUserFromDb.AddNewInsight(message.Text);
                        }
                        await db.SaveChangesAsync(); // сохранение 
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Инсайт сохранен");

                    }


                }

            }
        }
    }


}