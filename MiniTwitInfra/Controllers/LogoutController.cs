using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace MiniTwitInfra.Controllers
{
    public class LogoutController : PageModel
    {

        public LogoutController()
        {
        }

        public IActionResult OnGet()
        {
            HttpContext.Session.Remove(Authentication.AuthUsername);
            HttpContext.Session.Remove(Authentication.AuthId);
            HttpContext.Session.Remove(Authentication.AuthuserEmail);
            TempData["message"] = "You were logged out";

            return RedirectToPage("/Index");

        }

    }
}