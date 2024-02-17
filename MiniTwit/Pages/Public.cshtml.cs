using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models;

namespace MiniTwit.Pages;

public class PublicModel : PageModel
{
    private readonly int PER_PAGE = 30;
    private readonly ILogger<IndexModel> _logger;
    private readonly MiniTwitContext _context;

    public PublicModel(ILogger<IndexModel> logger, MiniTwitContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult OnGet()
    {
        var sample = _context.Twits.OrderByDescending(x => x.PubDate).Take(PER_PAGE).ToList();

        var outcome = sample
            .Join(
                _context.Users,
                message => message.AuthorId,
                user => user.Id,
                (message, user) =>
                    new TwitViewModel
                    {
                        AuthorUsername = user.UserName,
                        Text = message.Text,
                        PubDate = message.PubDate,
                        GravatarString = Utility.GetGravatar(user.Email, 48)
                    }
            )
            .ToList();

        if (TempData.ContainsKey("message"))
        {
            ViewData["message"] = TempData["message"];
        }
        ViewData["twits"] = outcome;

        return Page();
    }
}
