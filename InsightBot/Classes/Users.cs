using Microsoft.EntityFrameworkCore;

namespace Insight_bott;

public class Users
{
    public static void GetUser(
        long currentUserTgId, 
        in CancellationToken token, 
        in ApplicationContext db,
        out User? currentUserFromDb)
    {
        // получаем пользователя вместе с его список инсайтов
        // подробнее про то как работает здесь https://metanit.com/sharp/efcore/3.8.php
        var user = db.Users.Find(currentUserTgId);
        db.Entry(user).Collection(c => c.Insights).Load();
        currentUserFromDb = user;
    }
}