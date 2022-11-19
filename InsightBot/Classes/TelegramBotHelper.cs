using Telegram.Bot;
namespace Insight_bott;

public class TelegramBotHelper
{
    public static TelegramBotClient Client;

    public TelegramBotHelper(string telegramToken)
    {
        Client = new TelegramBotClient(telegramToken);
    }
}