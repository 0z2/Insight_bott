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
                try
                {
                    user.GetCurrentThought(out string textOfCurrentInsight, out int idInsightInDb);
                    AnswersMethods.SendInsight(textOfCurrentInsight, idInsightInDb, user.TelegramId);                   
                }
                catch(ArgumentOutOfRangeException)
                {
                    // если у пользователя нет ни одного инсайта
                    await TelegramBotHelper.Client.SendTextMessageAsync(
                        user.TelegramId,
                        "У вас не сохранено ни одного инсайта.\n" +
                        "Для добавления нового инасайте нажмите на /add_new_insight " +
                        "и затем напишите текст инсайта.");
                }
                //сохраняем чтобы изменился последний инсайт пользователя
                //DbHelper.db.SaveChangesAsync();
            }
        }
    }
}

    