using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models;

namespace MiniTwit.Pages.Autentication;

public class LoginModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly MiniTwitContext _context;

    public LoginModel(ILogger<IndexModel> logger, MiniTwitContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(IFormCollection request)
    {
        // Method for getting form fields
        var formCollection = Request.Form;
        string fieldName = formCollection["username"].ToString();

        User user;
        try
        {
            user = _context.Users.Where(x => x.UserName == fieldName).First();
        }
        catch (Exception ex)
        {
            ViewData["error"] = "No user Found";
            return Page();
        }

        HttpContext.Session.SetString(Security.Authentication.AuthUsername, user.UserName);
        HttpContext.Session.SetString(Security.Authentication.AuthId, user.Id.ToString());
        HttpContext.Session.SetString(Security.Authentication.AuthuserEmail, user.Email);

        return RedirectToPage("/Index");
    }
}
