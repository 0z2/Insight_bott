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
            
            if (message.Text != null)
            {
                Console.WriteLine($" {message.Chat.Id}");
                if (message.Text.ToLower().Contains("здорова"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Здоровей видали");
                }
                else if (message.Text.ToLower() == "/start")
                {
                    var isAlreadyInBase = false;
                    
                    // работаем с пользователем нажавшим start
                    await using (ApplicationContext db = new ApplicationContext()) //подключаемся к контексту БД
                    {
                        // получаем список пользователей из БД
                        var users = db.Users.ToList();
                        // проверяем есть ли пользователь уже в базе
                        foreach (User u in users)
                        {
                            if (u.Id == currentUserTgId)
                            {
                                isAlreadyInBase = true;
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже есть в списке пользователей.");
                            }
                        }
                        //если пользователя нет в базе, тогда добавляем
                        if (isAlreadyInBase == false)
                        {
                            Insight_bott.User newUser = new Insight_bott.User(currentUserTgId);
                            db.Users.AddRange(newUser); // добавляем в таблицу нового пользователя
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Вы были добавлены в список пользователей.");
                            // сохраняем изменения в таблице
                            await db.SaveChangesAsync(token); // разобраться что это за токен такой и для чего он нужен
                        }

                    }
                    
                }
                
                else if (message.Text.ToLower() == "/get_thought")
                {
                    User currentUserFromDb = null; //юзер который запросил мысль
                    
                    // получение данных
                    await using (ApplicationContext db = new ApplicationContext())
                    {
                        // получаем объекты из бд и выводим на консоль
                        var users = db.Users.ToList();
                        Console.WriteLine("Users list:");
                        foreach (User u in users)
                        {
                            Console.WriteLine($"{u.Id} - при нажатии /get_thought");
                        }
                        
                        //находим юзера, который запросил мысль
                        foreach (var user in users)
                        {
                            if (user.Id == currentUserTgId)
                            {
                                currentUserFromDb = user;
                            }
                        }

                        if (currentUserFromDb == null) // заплатка на случай если пользователя нет в списке пользователей, но он отправил сообщение
                        {
                            Message message2 = await Client.SendTextMessageAsync(
                                chatId: 985485455,
                                text: $"Пользователь которого нет в базе запросил мыль. tgId пользователя: {currentUserTgId}");
                        }
                        else
                        {
                            string currentUserThought = currentUserFromDb.GetCurrentThought();
                            await botClient.SendTextMessageAsync(message.Chat.Id, currentUserThought);
                        }

                    }
                }
            }
        }
    }


}