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
        public LogoutController() { }

        public IActionResult Index()
        {
            HttpContext.Session.Remove(Authentication.AuthUsername);
            HttpContext.Session.Remove(Authentication.AuthId);
            HttpContext.Session.Remove(Authentication.AuthuserEmail);
            TempData["message"] = "You were logged out";

            return RedirectToAction(controllerName: "Home", actionName: "Index");
        }
    }
}
