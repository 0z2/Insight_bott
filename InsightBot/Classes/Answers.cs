using Microsoft.EntityFrameworkCore;
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

            await botClient.SendTextMessageAsync(message.Chat.Id, "Вы были добавлены в список пользователей.");
            // сохраняем изменения в таблице
            await DbHelper.db.SaveChangesAsync(token); // разобраться что это за токен такой и для чего он нужен
        }
    }

    public static async void GetInsight(
        ITelegramBotClient сlient,
        Message message,
        long currentUserTgId,
        CancellationToken token)
    {
        // тут можно переписать чтобы сразу корректно подтягивались данные
        var currentUserFromDb = DbHelper.db.Users.Find(currentUserTgId); //юзер который запросил мысль
        DbHelper.db.Entry(currentUserFromDb).Collection(c => c.Insights).Load();
        if (currentUserFromDb ==
            null) // заплатка на случай если пользователя нет в списке пользователей, но он отправил сообщение
        {
            await сlient.SendTextMessageAsync(
                chatId: 985485455,
                text: $"Пользователь которого нет в базе запросил мыcль. tgId пользователя: {currentUserTgId}");
        }
        
        // получаем последний инсайт
        currentUserFromDb.GetCurrentThought(out string textOfCurrentUserInsight, out int idInsightInDb);
        
        // создаем инлайн кнопку для удаления инсайта
        InlineKeyboardButton deleteButton = new InlineKeyboardButton("Удалить");
        deleteButton.Text = "Удалить";
        deleteButton.CallbackData = Convert.ToString(idInsightInDb);
        InlineKeyboardMarkup inline = new InlineKeyboardMarkup(deleteButton);
        
        // отправляем текст инсайта с инлайн кнопкой удаления
        await сlient.SendTextMessageAsync(message.Chat.Id, textOfCurrentUserInsight, replyMarkup: inline);
        
        // сохранение для изменения номера последней мысли
        await DbHelper.db.SaveChangesAsync(token); 
    }
}