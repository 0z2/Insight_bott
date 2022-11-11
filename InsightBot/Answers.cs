using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Insight_bott;

public static class AnswersMethods
{

    public static async void Start(ITelegramBotClient botClient, Message message, long currentUserTgId, CancellationToken token)
    {
        var isAlreadyInBase = false;
                    
        // работаем с пользователем нажавшим start
        await using (ApplicationContext db = new ApplicationContext()) //подключаемся к контексту БД
        {
            // получаем список пользователей из БД
            var users = db.Users.ToList();
            // проверяем есть ли пользователь уже в базе
            foreach (User u in users)
            {
                if (u.TelegramId == currentUserTgId)
                {
                    isAlreadyInBase = true;
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже есть в списке пользователей.");
                }
            }
            //если пользователя нет в базе, тогда добавляем
            if (isAlreadyInBase == false)
            {
                Insight_bott.User newUser = new Insight_bott.User(currentUserTgId);
                db.Users.AddRange(newUser); // добавляем в таблицу нового пользователя. 
                await botClient.SendTextMessageAsync(message.Chat.Id, "Вы были добавлены в список пользователей.");
                // сохраняем изменения в таблице
                await db.SaveChangesAsync(token); // разобраться что это за токен такой и для чего он нужен
            }

        }

    }
    public static async void Zdorova(ITelegramBotClient botClient, Message message)
    {
        await botClient.SendTextMessageAsync(message.Chat.Id, "Здоровей видали");
    }

    public static async void GetThought(TelegramBotClient сlient, ITelegramBotClient botClient, Message message,
        long currentUserTgId, CancellationToken token)
    {
        await using (ApplicationContext db = new ApplicationContext())
        {
            User currentUserFromDb = Users.GetUser(currentUserTgId, token, db); //юзер который запросил мысль

            if (currentUserFromDb == null) // заплатка на случай если пользователя нет в списке пользователей, но он отправил сообщение
            {
                Message message2 = await сlient.SendTextMessageAsync(
                    chatId: 985485455,
                    text: $"Пользователь которого нет в базе запросил мыcль. tgId пользователя: {currentUserTgId}");
            }
            else
            {
                string currentUserThought = currentUserFromDb.GetCurrentThought();

                await botClient.SendTextMessageAsync(message.Chat.Id, currentUserThought);
            }

            await db.SaveChangesAsync(token); // сохранение для изменения номера последней мысли
        }

    }
}


