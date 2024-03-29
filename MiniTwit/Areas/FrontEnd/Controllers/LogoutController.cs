using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [Area("FrontEnd")]
    public class LogoutController : Controller
    {
        private readonly ILogger<LogoutController> _logger;
        public LogoutController(ILogger<LogoutController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {

                HttpContext.Session.Remove(Authentication.AuthUsername);
                HttpContext.Session.Remove(Authentication.AuthId);
                HttpContext.Session.Remove(Authentication.AuthuserEmail);
                TempData["message"] = "You were logged out";

                _logger.LogInformation("User logged out successfully");
                return RedirectToAction(controllerName: "Home", actionName: "Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing Logout action");
                throw;
            }
        }
    }
}
