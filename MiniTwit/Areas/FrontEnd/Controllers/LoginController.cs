using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [IgnoreAntiforgeryToken]
    [Area("FrontEnd")]
    public class LoginController : Controller
    {
        private readonly MiniTwitContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public LoginController(MiniTwitContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var tempData = TempData["message"];
            if (tempData != null)
            {
                ViewData["message"] = tempData.ToString();
            }

            return View();
        }

        [HttpPost]
        public IActionResult Index(IFormCollection request)
        {
            // Method for getting form fields
            var formCollection = Request.Form;
            string fieldName = formCollection["username"].ToString();
            string fieldPassword = formCollection["password"].ToString();

            User user;
            try
            {
                user = _context.Users.Where(x => x.UserName == fieldName).First();
            }
            catch
            {
                ViewData["error"] = "Invalid username";
                return View();
            }

            PasswordVerificationResult passwordVerificationResult;

            string storedPasswordHash;

            // This is to remove the werkzeug format
            // In any case, we do not suport login of the old users (yet)
            if (user.PasswordHash.Contains('$'))
            {
                storedPasswordHash = user.PasswordHash.Split('$')[2];
            }
            else
            {
                storedPasswordHash = user.PasswordHash;
            }

            passwordVerificationResult = _passwordHasher.VerifyHashedPassword(
                user: user,
                hashedPassword: storedPasswordHash,
                providedPassword: fieldPassword
            );

            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                ViewData["error"] = "Invalid password";
                return View();
            }

            HttpContext.Session.SetString(Authentication.AuthUsername, user.UserName);
            HttpContext.Session.SetString(Authentication.AuthId, user.Id.ToString());
            HttpContext.Session.SetString(Authentication.AuthuserEmail, user.Email);

            TempData["message"] = "You were logged in";

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }
    }
}
