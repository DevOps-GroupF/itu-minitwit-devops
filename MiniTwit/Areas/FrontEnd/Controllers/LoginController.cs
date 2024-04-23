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
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            MiniTwitContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<LoginController> logger
        )
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                var tempData = TempData["message"];
                if (tempData != null)
                {
                    ViewData["message"] = tempData.ToString();
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while processing GET request for Index action"
                );
                throw;
            }
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
                IEnumerable<User> users = _context.Users;

                if (!users.Any())
                {
                    _logger.LogWarning($"User '{fieldName}' not found");
                    ViewData["error"] = "Invalid username";
                    return View();
                }

                var usersWithUsername = users.Where(x => x.UserName == fieldName);

                user = usersWithUsername.FirstOrDefault(defaultValue: null);

                if (user == null)
                {
                    _logger.LogWarning($"User '{fieldName}' not found");
                    ViewData["error"] = "Invalid username";
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while retrieving user information from the database"
                );
                ViewData["error"] = "Error logging in";
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
                _logger.LogWarning($"Invalid password for user '{fieldName}'");
                ViewData["error"] = "Invalid password";
                return View();
            }

            HttpContext.Session.SetString(Authentication.AuthUsername, user.UserName);
            HttpContext.Session.SetString(Authentication.AuthId, user.Id.ToString());
            HttpContext.Session.SetString(Authentication.AuthuserEmail, user.Email);

            TempData["message"] = "You were logged in";
            _logger.LogInformation($"User '{fieldName}' logged in successfully");
            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }
    }
}
