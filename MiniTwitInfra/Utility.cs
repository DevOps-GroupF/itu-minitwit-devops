using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models;

namespace MiniTwitInfra
{
    public class Utility
    {
        public static string GetGravatar(string email, int size = 80)
        {
            MD5 hasher = MD5.Create();
            byte[] bytes_data = hasher.ComputeHash(Encoding.Default.GetBytes(email));
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < bytes_data.Length; i++)
            {
                result.Append(bytes_data[i].ToString("x2"));
            }
            return "http://www.gravatar.com/avatar/"
                + result.ToString()
                + "?d=identicon&s="
                + size.ToString();
        }

        public static int GetUserIdFromHttpSession(HttpContext httpContext)
        {
            string idFromSession = httpContext.Session.GetString(Authentication.AuthId);

            int id;

            if (idFromSession == null || !int.TryParse(idFromSession, out id))
            {
                throw new System.Security.Authentication.AuthenticationException(
                    "No user logged in"
                );
            }

            return id;
        }

        public static async Task<bool> ValidUserIsLoggedIn(
            HttpContext httpContext,
            MiniTwitContext miniTwitContext
        )
        {
            if (SessionHasUserId(httpContext))
            {
                int userId = GetUserIdFromHttpSession(httpContext);

                return await User.UserWithIdExists(userId, miniTwitContext);
            }
            else
            {
                return false;
            }
        }

        public static bool SessionHasUserId(HttpContext httpContext)
        {
            return null != httpContext.Session.GetString(Authentication.AuthId);
        }
    }
}
