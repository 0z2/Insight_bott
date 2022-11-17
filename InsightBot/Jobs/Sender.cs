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
        }
    }
}

    