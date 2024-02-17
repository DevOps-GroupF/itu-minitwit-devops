using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

/* create table user ( */
/*   user_id integer primary key autoincrement, */
/*   username string not null, */
/*   email string not null, */
/*   pw_hash string not null */
/* ); */

namespace MiniTwitInfra.Models
{
    [Table("user")]
    public class User
    {
        [Column("user_id")]
        public int Id { get; set; }

        [Column("username")]
        public string UserName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("pw_hash")]
        public string PasswordHash { get; set; }

        public static async Task<User> GetUserFromUserIdAsync(
            int userId,
            Data.MiniTwitContext miniTwitContext
        )
        {
            User user;

            try
            {
                user = await miniTwitContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception e)
            {
                throw new System.Security.Authentication.AuthenticationException(e.Message);
            }

            if (user == null)
            {
                throw new System.Security.Authentication.AuthenticationException("User not found");
            }

            return user;
        }

        public static async Task<bool> UserWithIdExists(
            int userId,
            Data.MiniTwitContext miniTwitContext
        )
        {
            User user = await miniTwitContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            return null != user;
        }
    }
}
