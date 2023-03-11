using System.Diagnostics;
using Insight_bott.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Telegram.Bot;
using Exception = System.Exception;

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
            
            // старый вариант загрузки через два запроса к базе
            //var currentUsersFromDb = DbHelper.db.Users.ToList(); //юзер который запросил мысль
            //DbHelper.db.Insights.ToList(); // это чтобы подтянулись в контекст инсайты пользователей
            // новый вариант загрузки одним запросом
            var currentUsersFromDb = DbHelper.db.Users.Include(x => x.Insights).ToList();
            
            // рассылаем ежедневные инсайты пользователям
            foreach (User user in currentUsersFromDb)
            {
                // если пользователь заблокировал отправку - пропускаем
                if (user.isAsign == false) continue;
                try
                {
                    // ежедневный инсайт
                    user.GetCurrentInsight(out string textOfCurrentInsight, out int idInsightInDb);
                    AnswersMethods.SendInsight(textOfCurrentInsight, idInsightInDb, user.TelegramId);

                    // проссматриваем инсайты
                    foreach (Insight insight in user.Insights)
                    {
                        // инсайты с сегодняшней датой повторения, за исключением ежедневного инсайта
                        if (insight.WhenToRepeat == DateTime.Today && insight.Id != idInsightInDb)
                        {
                            AnswersMethods.SendInsight(insight.TextOfInsight, insight.Id, user.TelegramId);
                            insight.WhenToRepeat = null;
                            insight.DayOfLastRepeat = DateTime.Today;
                        }

                        // если установлено регулярное повторение
                        if (insight.HowOftenRepeatInDays is not null)
                        {
                            TimeSpan difference = DateTime.Today - insight.DayOfLastRepeat.GetValueOrDefault();
                            int differenceInDays = difference.Days;
                            // если количество дней с даты последнего повторения больше или равно дней регулярности повторения
                            if (differenceInDays >= insight.HowOftenRepeatInDays)
                            {
                                AnswersMethods.SendInsight(insight.TextOfInsight, insight.Id, user.TelegramId);
                                insight.DayOfLastRepeat = DateTime.Today;
                            }
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // если у пользователя нет ни одного инсайта
                    AnswersMethods.SendMessage(user.TelegramId,
                        "У вас не сохранено ни одного инсайта.\n" +
                        "Для добавления нового инасайте нажмите на /add_new_insight " +
                        "и затем напишите текст инсайта.",
                        out int idOfMessage);
                }

            }
            await DbHelper.db.SaveChangesAsync();
        }
    }
}

    