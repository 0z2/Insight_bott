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
            DbHelper.db.Insights.ToList(); // это чтобы подтянулись в контекст инсайты пользователей
            
            // рассылаем инсайты пользователям
            foreach (User user in currentUsersFromDb)
            {
                AnswersMethods.GetInsight(TelegramBotHelper.Client, message, user.TelegramId);
            }
            // сохраняем чтобы изменился последний инсайт пользователя
            DbHelper.db.SaveChangesAsync();
        }
    }
}

    