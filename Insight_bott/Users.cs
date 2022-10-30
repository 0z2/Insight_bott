using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight_bott
{
    public class Users
    {
        public List<User> ListOfUsers;

        public Users()
        {
            ListOfUsers = new List<User>();
        }

        public User ReturnUser(long tgId)
        {
            foreach (var user in ListOfUsers)
            {
                if (user.UserTelegramId == tgId)
                {
                    return user;
                }
            }
            return null;
        }
    }
}
