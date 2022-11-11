namespace Insight_bott;

public class Users
{
    public static User GetUser(long currentUserTgId, CancellationToken token, ApplicationContext db)
    {
        // получение данных
        var users = db.Users.ToList();
        var insights = db.Insights.ToList();
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