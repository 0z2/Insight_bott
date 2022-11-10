namespace Insight_bott;

public class Users
{
    public static User GetUser(long currentUserTgId, CancellationToken token, ApplicationContext db)
    {
        // получение данных

        // получаем объекты из бд и выводим на консоль
        var users = db.Users.ToList();
        User currentUserFromDb = null;
        

        //находим юзера, который запросил мысль
        foreach (var user in users)
        {
            if (user.TelegramId == currentUserTgId)
            {
                currentUserFromDb = user;
                break;
            }
        }
        return currentUserFromDb;
        
    }
}