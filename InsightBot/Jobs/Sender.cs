using Quartz;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Data.Entity;
using System.Threading.Tasks;

namespace Insight_bott.Jobs
{
    public class Sender : IJob
    {

        TelegramBotClient Client;

        public Sender()
        {
            // эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
            DotNetEnv.Env.TraversePath().Load();
            string telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
            
            Client = new TelegramBotClient(telegramBotApiKey);
            Client.StartReceiving(Update, Error);
        }

        
        public async Task Execute(IJobExecutionContext context)
        {
            Message message = await Client.SendTextMessageAsync(
            chatId: 985485455,
            text: "Бот не упущен!");
        }
        static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }

        async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message.Text != null)
            {
                Console.WriteLine($" {message.Chat.Id}");
                if (message.Text.ToLower().Contains("здорова"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Здоровей видали");
                    //await botClient.SendTextMessageAsync(message.Chat.Id, user.GetCurrentThought()); //это как мысль отправить
                    return;
                }
                else if (message.Text.ToLower() == "/start")
                {
                    var userTelegramId = message.Chat.Id; // id того кто запросил мысль

                    // создаем нового пользователя и добавляем в DB
                    await using (ApplicationContext db = new ApplicationContext())
                    {
                        Console.WriteLine("Пользователь добавлен!");
                        Insight_bott.User newUser = new Insight_bott.User(userTelegramId); // добавить проверку на случай, если пользователь уже есть в базе
                        db.Users.AddRange(newUser);
                        await db.SaveChangesAsync(token);
                    }
                    
                    
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать!");
                    return;
                }
                
                else if (message.Text.ToLower() == "/get_thought")
                {
                    var currentUserTgId = message.Chat.Id;
                    User currentUser = null; //юзер который запросил мысль
                    
                    // получение данных
                    await using (ApplicationContext db = new ApplicationContext())
                    {
                        bool isAvalaible = db.Database.CanConnect();
                        // bool isAvalaible2 = await db.Database.CanConnectAsync();
                        if (isAvalaible) Console.WriteLine("База данных доступна");
                        else Console.WriteLine("База данных не доступна");
                        
                        // получаем объекты из бд и выводим на консоль
                        var users = db.Users.ToList();
                        Console.WriteLine("Users list:");
                        foreach (User u in users)
                        {
                            Console.WriteLine($"{u.Id} - при нажатии /get_thought");
                        }
                        
                        foreach (var user in users)
                        {
                            if (user.Id == currentUserTgId)
                            {
                                currentUser = user;
                            }
                        }

                        if (currentUser == null) // заплатка на случай если пользователя нет в списке пользователей, но он отправил сообщение
                        {

                        }
                        else
                        {
                            string currentUserThought = currentUser.GetCurrentThought();
                            await botClient.SendTextMessageAsync(message.Chat.Id, currentUserThought);
                        }

                    }
                    }


            }
        }
    }


}