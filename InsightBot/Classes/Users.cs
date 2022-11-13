namespace Insight_bott;

public class Users
{
    public static void GetUser(
        in long currentUserTgId, 
        in CancellationToken token, 
        in ApplicationContext db,
        out User? currentUserFromDb)
    {
        // получение данных
        var users = db.Users.ToList();
        currentUserFromDb = null;

        //находим юзера, который запросил мысль
        foreach (var user in users)
        {
            if (user.TelegramId == currentUserTgId)
            {
                currentUserFromDb = user;
                break;
            }
        }
    }
}