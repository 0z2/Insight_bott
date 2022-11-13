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
        User? user = db.Users.Find(currentUserTgId);
        currentUserFromDb = user;
    }
}