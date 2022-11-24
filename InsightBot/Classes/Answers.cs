using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insight_bott;

public static class AnswersMethods
{
    public static async void Start(
        ITelegramBotClient botClient,
        Message message,
        long currentUserTgId,
        CancellationToken token)
    {
        var isAlreadyInBase = false;

        // работаем с пользователем нажавшим start

        // пытаемся найти пользователя
        var user = DbHelper.db.Users.Find(currentUserTgId);

        // если пользователь существует сообщаем об это
        if (user is not null)
        {
            isAlreadyInBase = true;
            await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже есть в списке пользователей.");
        }
        //если пользователя нет в базе, тогда добавляем
        else if (isAlreadyInBase == false)
        {
            Insight_bott.User newUser = new Insight_bott.User(currentUserTgId);
            // добавляем стартовый набор инсайтов
            var startInsights = new List<string>()
            {
                "Глаза боятся - руки делают!", "Тише едешь - дальше будешь!", "Утро вечера мудренее!"
            };
            foreach (var textOfInsight in startInsights)
            {
                newUser.AddNewInsight(textOfInsight);
            }

            DbHelper.db.Users.Add(newUser); // добавляем в таблицу нового пользователя

            await botClient.SendTextMessageAsync(
                message.Chat.Id, "Вы были добавлены в список пользователей.\n" +
                                 "В этом боте вы можете сохранять значимые для себя мысли. " +
                                 "Каждое утро бот будет присылать по одной из них.\n" +
                                 "Для добавления мысли нажмите /add_new_insight");
            // сохраняем изменения в таблице
            await DbHelper.db.SaveChangesAsync(token); // разобраться что это за токен такой и для чего он нужен
        }
    }
    public static void GetInsight(
        long currentUserTgId,
        out string textOfInsight,
        out int idOfUserInsightInDb
        )
    {
        // тут можно переписать чтобы сразу корректно подтягивались данные
        var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
        DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();

        // получаем последний инсайт
        currentUserFromDb.GetCurrentInsight(out string textOfCurrentUserInsight, out int idOfCurrentUserInsightInDb);
        
        // возвращаем
        textOfInsight = textOfCurrentUserInsight;
        idOfUserInsightInDb = idOfCurrentUserInsightInDb;
    }

    public static async void SendInsight(
        string textOfCurrentUserInsight,
        int idInsightInDb,
        long currentUserTgId)
    {
             // создаем инлайн кнопку для удаления инсайта
            InlineKeyboardButton deleteButton = new InlineKeyboardButton("Удалить");
            deleteButton.Text = "Удалить";
            deleteButton.CallbackData = Convert.ToString(idInsightInDb);
            InlineKeyboardMarkup inline = new InlineKeyboardMarkup(deleteButton);
        
            // отправляем текст инсайта с инлайн кнопкой удаления
            await TelegramBotHelper.Client.SendTextMessageAsync(currentUserTgId, textOfCurrentUserInsight, replyMarkup: inline);
    }
}