using Quartz;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Insight_bott.Jobs
{
    public class Sender : IJob
    {
        Users users;
        TelegramBotClient Client;

        public Sender()
        {
            // эта штука достает переменные из env файла. Вроде как env файл должен лежать в корне
            DotNetEnv.Env.TraversePath().Load();
            string telegramBotApiKey = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
            
            users = new Users();
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
                    long userTelegramId = message.Chat.Id;
                    Insight_bott.User newUser = new Insight_bott.User(userTelegramId); // добавить проверку на случай, если пользователь уже есть в базе
                    users.ListOfUsers.Add(newUser);
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать!");
                    return;
                }
                else if (message.Text.ToLower() == "/get_thought")
                {
                    Insight_bott.User currentUser = users.ReturnUser(message.Chat.Id);
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