using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwit.Security;


namespace MiniTwit.Pages;

public class LogoutModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
   
    public LogoutModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
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
