using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models.DataModels;

namespace MiniTwitInfra.Controllers;

[IgnoreAntiforgeryToken]
public class LoginController : PageModel
{
    private readonly MiniTwitContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public LoginController(
        MiniTwitContext context,
        IPasswordHasher<User> passwordHasher
    )
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public IActionResult OnGet()
    {
        var tempData = TempData["message"];
        if (tempData != null)
        {
            ViewData["message"] = tempData.ToString();
        }
        return Page();
    }

    public IActionResult OnPostAsync(IFormCollection request)
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
            return Page();
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
            return Page();
        }

        HttpContext.Session.SetString(Authentication.AuthUsername, user.UserName);
        HttpContext.Session.SetString(Authentication.AuthId, user.Id.ToString());
        HttpContext.Session.SetString(Authentication.AuthuserEmail, user.Email);

        TempData["message"] = "You were logged in";

        return RedirectToPage("/Index");
    }
}
