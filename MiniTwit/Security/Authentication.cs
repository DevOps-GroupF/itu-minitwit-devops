using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Session;

namespace MiniTwit.Security
{
    public class Authentication
    {
        public const string AuthId = "userid";
        public const string AuthUsername = "username";
        public const string AuthuserEmail = "useremail";
    }
}
