using Quartz.Impl;
using Quartz;


namespace Insight_bott.Jobs
{
    public class Sheduler
    {
        public async void Start()
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
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(10, 30))
                .Build();

            // начинаем выполнение работы
            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
