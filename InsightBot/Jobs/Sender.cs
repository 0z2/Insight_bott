using System.Data.Entity;
using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace Insight_bott.Jobs
{
    public class Sender : IJob
    {
        public Sender()
        {
            
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            Message message = await TelegramBotHelper.Client.SendTextMessageAsync(
                chatId: 985485455, // скрыть админский id
                text: "Ежедневная отправка инсайтов пользователям!");
            
            // тут можно переписать чтобы сразу корректно подтягивались данные
            var currentUsersFromDb = DbHelper.db.Users.ToList(); //юзер который запросил мысль
            DbHelper.db.Insights.ToList();
            foreach (User user in currentUsersFromDb)
            {
                Console.WriteLine($"Отправляем ежедневный инсайт пользователю {user.TelegramId}");
                
                // получаем последний инсайт пользователя
                user.GetCurrentThought(out string textOfCurrentUserInsight, out int idInsightInDb);
                await TelegramBotHelper.Client.SendTextMessageAsync(
                    chatId: user.TelegramId, // скрыть админский id
                    text: textOfCurrentUserInsight);
            }
        }
    }
}

    