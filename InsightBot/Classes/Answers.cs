using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insight_bott;

public delegate void AdminNotifier(string message);
public static class AnswersMethods
{
    // Создаем переменную делегата
    public static AdminNotifier? AdminNotifier;
    // Регистрируем делегат
    public static void RegisterNotifier(AdminNotifier func)
    {
        AdminNotifier += func;
    }
    public static void SendMessage(
        long UserTgId,
        string textOfMessage,
        out int idOfMessage,
        InlineKeyboardMarkup? replyMarkup = null
        )
    {
        idOfMessage = 0;
        try
        {
            if (replyMarkup is null)
            {
                var sentMessage = TelegramBotHelper.Client.SendTextMessageAsync(
                    UserTgId, textOfMessage);
                idOfMessage = sentMessage.Result.MessageId;
            }
            else
            {
                var sentMessage =  TelegramBotHelper.Client.SendTextMessageAsync(
                    UserTgId, 
                    textOfMessage,
                    replyMarkup: replyMarkup
                    );
                idOfMessage = sentMessage.Result.MessageId;
            }

        }
        catch (Exception e)
        {
            if (e.Message == "Forbidden: bot was blocked by the user"
                || e.Message == "One or more errors occurred. (Forbidden: bot was blocked by the user)"
                || e.Message == "One or more errors occurred. (Forbidden: user is deactivated)")
            {
                // отмечаем что юзер заблокировал сообщения
                var UserFromDb = DbHelper.db.Users.Find(UserTgId); //юзер который запросил мысль
                UserFromDb.isAsign = false;
                DbHelper.db.SaveChangesAsync();
                
                // логируем запрос
                Logging.ServiceProvider.Logger.Write(
                    $"Юзер c id {UserFromDb.TelegramId} заблокировал сообщения");
            }
            else
            {
                throw;
            }
        }

    }
    
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
            SendMessage(message.Chat.Id, "Бот включен", out int idOfMessage);
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

            try
            {
                AnswersMethods.SendMessage(
                    message.Chat.Id,
                    "Вы были добавлены в список пользователей.\n" +
                    "В этом боте вы можете сохранять значимые для себя мысли. " +
                    "Каждое утро бот будет присылать по одной из них.\n" +
                    "Для добавления мысли нажмите /add_new_insight",
                    out int idOfMessage);
            }
            catch (Exception e)
            {
                newUser.isAsign = false;
            }
            
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
        // получаем пользователя и его список инсайтов из DB
        var currentUserFromDb =
            DbHelper.db.Users
                .Include(user => user.Insights)
                .FirstOrDefault(user => user.TelegramId == currentUserTgId);

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
        long currentUserTgId)
    {
        // добавляем инлйн кнопки. Если их не добавить, то и изменить в дальнейшем не получится
        CreateBaseInlineButtons(idInsightInDb, out InlineKeyboardMarkup baseInlineKeyboard);
        // отправляем текст инсайта
        SendMessage(currentUserTgId, textOfCurrentUserInsight, out int messageId, baseInlineKeyboard);
        
        // создаем базовые инлайн кнопки с информацией о id сообщения чтобы можно было редактировать кнопки и отправляем
        CreateBaseInlineButtons(idInsightInDb, out baseInlineKeyboard, messageId);
        TelegramBotHelper.Client.EditMessageReplyMarkupAsync(currentUserTgId, messageId, replyMarkup: baseInlineKeyboard);
    }


    public static void CreateBaseInlineButtons(int idInsightInDb, out InlineKeyboardMarkup inlineKeyboard, int? messageId=null)
    {
        var insight = DbHelper.db.Insights.Find(idInsightInDb);
        var insightHasRepeat = insight.HowOftenRepeatInDays != null;
        
        // создаем кнопки инсайтов
        var regularRepeatButton = new InlineKeyboardButton("Регулярное повторение");
        regularRepeatButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Регулярное повторение" + "," + messageId);
            
        var singleRepititionButton = new InlineKeyboardButton("Разовое повторение");
        singleRepititionButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Разовое повторение"  + "," + messageId);
        
        var deleteRepititionButton = new InlineKeyboardButton("Отключить повторения");
        deleteRepititionButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Отключить повторения" + "," + messageId);
        
        var deleteInsight = new InlineKeyboardButton("Удалить");
        deleteInsight.CallbackData = Convert.ToString(idInsightInDb + "," + "Удалить" + "," + messageId);

        var row1 = new List<InlineKeyboardButton> { regularRepeatButton, singleRepititionButton };
        var row2 = new List<InlineKeyboardButton> { deleteRepititionButton };
        var row3 = new List<InlineKeyboardButton> { deleteInsight };

        List<List<InlineKeyboardButton>> keyboard = new List<List<InlineKeyboardButton>>();
        keyboard.Add(row1);
        if (insightHasRepeat)
        {
            keyboard.Add(row2);
        }
        keyboard.Add(row3);
        
        inlineKeyboard = new InlineKeyboardMarkup(keyboard);
    }
    public static void CreateSingleReptitionInlineButtons(int idInsightInDb, out InlineKeyboardMarkup inlineKeyboard, int? messageId=null)
    {
        // пример создания инлайн кнопок https://stackoverflow.com/questions/62797191/how-to-add-two-inline-buttons-to-a-telegram-bot-by-c
        
        // создаем кнопки инсайтов
        InlineKeyboardButton backButton = new InlineKeyboardButton("Назад");
        backButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Назад" + "," + messageId);
            
        InlineKeyboardButton repeatTomorrowButton = new InlineKeyboardButton("Повторить завтра");
        repeatTomorrowButton.CallbackData = Convert.ToString(idInsightInDb) + "," + "Повторить завтра" + "," + messageId;

        InlineKeyboardButton repeatInADayButton = new InlineKeyboardButton("Повторить через день");
        repeatInADayButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Повторить через день" + "," + messageId);
            
        InlineKeyboardButton repeatInAWeekButton = new InlineKeyboardButton("Повторить через неделю");
        repeatInAWeekButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Повторить через неделю" + "," + messageId);

        InlineKeyboardButton[] row1 = new InlineKeyboardButton[] { repeatTomorrowButton, repeatInADayButton };
        InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { repeatInAWeekButton};
        InlineKeyboardButton[] row3 = new InlineKeyboardButton[] { backButton };
            
        inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            row1, row2, row3
        });
    }
    public static void CreateRegularReptitionInlineButtons(int idInsightInDb, out InlineKeyboardMarkup inlineKeyboard, int? messageId=null)
    {
        // пример создания инлайн кнопок https://stackoverflow.com/questions/62797191/how-to-add-two-inline-buttons-to-a-telegram-bot-by-c

        var insight = DbHelper.db.Insights.Find(idInsightInDb);
        var symbolOfActiveButtonEveryDay = "";
        var symbolOfActiveButtonInADay = "";
        var symbolOfActiveButtonAweek = "";
        
        switch (insight.HowOftenRepeatInDays)
        {
            case 1:
                symbolOfActiveButtonEveryDay = " ✅";
                break;
            case 2:
                symbolOfActiveButtonInADay = " ✅";
                break;
            case 7:
                symbolOfActiveButtonAweek = " ✅";
                break;
        }
        
        // создаем кнопки инсайтов
        InlineKeyboardButton backButton = new InlineKeyboardButton("Назад");
        backButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Назад" + "," + messageId);
            
        InlineKeyboardButton repeatDailyButton = new InlineKeyboardButton("Повторять ежедневно" + symbolOfActiveButtonEveryDay);
        repeatDailyButton.CallbackData = Convert.ToString(idInsightInDb) + "," + "Повторять ежедневно" + "," + messageId;

        InlineKeyboardButton repeatInADayButton = new InlineKeyboardButton("Повторять через день" + symbolOfActiveButtonInADay);
        repeatInADayButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Повторять через день" + "," + messageId);
            
        InlineKeyboardButton repeatWeeklyButton = new InlineKeyboardButton("Повторять еженедельно" + symbolOfActiveButtonAweek);
        repeatWeeklyButton.CallbackData = Convert.ToString(idInsightInDb + "," + "Повторять еженедельно" + "," + messageId);

        InlineKeyboardButton[] row1 = new InlineKeyboardButton[] { repeatDailyButton };
        InlineKeyboardButton[] row2 = new InlineKeyboardButton[] { repeatInADayButton };
        InlineKeyboardButton[] row3 = new InlineKeyboardButton[] { repeatWeeklyButton};
        InlineKeyboardButton[] row4 = new InlineKeyboardButton[] { backButton };
            
        inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            row1, row2, row3, row4
        });
    }
    
    // завел метод поглядеть как тесты работают
    public static int TestMethod(int a, int b)
    {
        return a + b;
    }
}