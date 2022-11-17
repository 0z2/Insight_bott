using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Insight_bott.Jobs
{
    public class Sheduler
    {
        public async void Start(TelegramBotClient client)
        {
            IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();
            
            // https://www.quartz-scheduler.net/documentation/quartz-2.x/tutorial/more-about-jobs.html#jobdatamap
            // где то тут есть инфа как передать контекст
            // !!! надо понять как сюда передать объект телеграм бота, чтобы через него шла отправка сообщений

            IJobDetail job = JobBuilder
                .Create<Sender>()
                .Build();

            ITrigger trigger = TriggerBuilder.Create() // создаем триггер
                .WithIdentity("trigger1", "group1") // идентифицируем триггер с именем и группой
                .StartNow() // запуск сразу после начала выполнения
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(02, 40))
                .Build();
            // .WithSimpleSchedule(x => x            // настраиваем выполнение действия
                //     .WithIntervalInMinutes(60)         // через 1 минуту
                //     .RepeatForever())                   // бесконечное повторение
                // .Build();

            await scheduler.ScheduleJob(job, trigger);        // начинаем выполнение работы
        }
    }
}
