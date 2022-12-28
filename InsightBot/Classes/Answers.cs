using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Exception = System.Exception;

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
            user.isAsign = true;
            await DbHelper.db.SaveChangesAsync(token);
            await botClient.SendTextMessageAsync(message.Chat.Id, "Бот включен");
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
        out int idOfUserInsightInDb,
        int idOfInsight = 0
        )
    {
        // тут можно переписать чтобы сразу корректно подтягивались данные
        var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
        DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();

        string textOfCurrentUserInsight;
        int idOfCurrentUserInsightInDb = 0;
        // если id инсайта, которых нужно получить не передано, то получаем текущий в порядке инсайт
        if (idOfInsight == 0)
        {
            // получаем последний инсайт
            currentUserFromDb.GetCurrentInsight(out textOfCurrentUserInsight, out idOfCurrentUserInsightInDb);
        }
        // в противном случае получаем инсайт по id
        else
        {
            currentUserFromDb.GetInsightById(idOfInsight, out textOfCurrentUserInsight);
        }
        
        // возвращаем
        textOfInsight = textOfCurrentUserInsight;
        idOfUserInsightInDb = idOfCurrentUserInsightInDb;
    }
    
    public static async void SendInsight(
        string textOfCurrentUserInsight,
        int idInsightInDb,
        long currentUserTgId,
        User? user = null)
    {

        CreateInlineButtons(idInsightInDb, out InlineKeyboardMarkup inlineKeyboard);

        try
        {
             // отправляем текст инсайта с инлайн кнопкой удаления
            await TelegramBotHelper.Client.SendTextMessageAsync(
                         currentUserTgId, 
                         textOfCurrentUserInsight, 
                         replyMarkup: inlineKeyboard);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException e)
        {
            if (e.Message == "Forbidden: bot was blocked by the user")
            {
                // отмечаем что юзер заблокировал сообщения
                user.isAsign = false;
                await DbHelper.db.SaveChangesAsync();
                
                // добавить логгирование
                Console.WriteLine(user.TelegramId + " заблокировал сообщения");
            }
            else
            {
                throw;
            }
        }
    }

    public static void CreateInlineButtons(int idInsightInDb, out InlineKeyboardMarkup inlineKeyboard)
    {
        // пример создания инлайн кнопок https://stackoverflow.com/questions/62797191/how-to-add-two-inline-buttons-to-a-telegram-bot-by-c
        // создаем инлайн кнопку для удаления инсайта
        InlineKeyboardButton deleteButton = new InlineKeyboardButton("Удалить");
        deleteButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Удалить");
            
        // создаем инлайн кнопку для удаления инсайта
        InlineKeyboardButton repeatTomorrowButton = new InlineKeyboardButton("Повторить завтра");
        repeatTomorrowButton.CallbackData = Convert.ToString(idInsightInDb) + "," + "Повторить завтра";

        // создаем инлайн кнопку для удаления инсайта
        InlineKeyboardButton repeatInADayButton = new InlineKeyboardButton("Повторить через день");
        repeatInADayButton.CallbackData = Convert.ToString(idInsightInDb) + "," + "Повторить через день";
            
        // создаем инлайн кнопку для удаления инсайта
        InlineKeyboardButton repeatInAWeekButton = new InlineKeyboardButton("Повторить через неделю");
        repeatInAWeekButton.CallbackData = Convert.ToString(idInsightInDb) + "," + "Повторить через неделю";

        InlineKeyboardButton[] row1 = new InlineKeyboardButton[] { repeatTomorrowButton, repeatInADayButton };
        InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { repeatInAWeekButton};
        InlineKeyboardButton[] row3 = new InlineKeyboardButton[] { deleteButton };
            
        inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            row1, row2, row3
        });
    }
    
    // завел метод поглядеть как тесты работают
    public static int TestMethod(int a, int b)
    {
        return a + b;
    }
}