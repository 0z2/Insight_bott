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
            DotNetEnv.Env.TraversePath().Load();
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");
            await TelegramBotHelper.Client.SendTextMessageAsync(
                chatId: adminId, // скрыть админский id
                text: "Ежедневная отправка инсайтов пользователям!");
            
            // тут можно переписать чтобы сразу корректно подтягивались данные
            var currentUsersFromDb = DbHelper.db.Users.ToList(); //юзер который запросил мысль
            DbHelper.db.Insights.ToList(); // это чтобы подтянулись в контекст инсайты пользователей
            
            // рассылаем инсайты пользователям
            foreach (User user in currentUsersFromDb)
            {
                try
                {
                    user.GetCurrentInsight(out string textOfCurrentInsight, out int idInsightInDb);
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
            }
        }
    }
}

    