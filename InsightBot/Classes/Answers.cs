using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

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
        await using (ApplicationContext db = new ApplicationContext()) //подключаемся к контексту БД
        {
            // пытаемся найти пользователя
            var user = db.Users.Find(currentUserTgId);
            
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
                db.Users.Add(newUser); // добавляем в таблицу нового пользователя
                
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

    public static async void GetInsight(
        TelegramBotClient сlient,
        ITelegramBotClient botClient,
        Message message,
        long currentUserTgId,
        CancellationToken token)
    {
        await using (ApplicationContext db = new ApplicationContext())
        {
            Users.GetUser(currentUserTgId, in token, in db, out User currentUserFromDb); //юзер который запросил мысль
            //var insights = db.Insights.ToList();
            // ??? Почему если эта строка отсутствует, то список инсайтов не отображается коректно???
            // если ее не писать, то отображается три инсайта добавленных при создании юзера
            // в дальнейшем добавленные инсайты добавляются в базу, но при запросе юзера не выводятся
            // причем список инсайтов судя по всему в юзера добавляется дополнительный (то есть вместо трех инсайтов
            // которые действительно есть у юзера после вывоза списка инсайтов их становится семь

            if (currentUserFromDb == null) // заплатка на случай если пользователя нет в списке пользователей, но он отправил сообщение
            {
                await сlient.SendTextMessageAsync(
                    chatId: 985485455,
                    text: $"Пользователь которого нет в базе запросил мыcль. tgId пользователя: {currentUserTgId}");
            }
            else
            {
                currentUserFromDb.GetCurrentThought(out string textOfCurrentUserInsight);

                await botClient.SendTextMessageAsync(message.Chat.Id, textOfCurrentUserInsight);
            }

            await db.SaveChangesAsync(token); // сохранение для изменения номера последней мысли
        }

    }
}


