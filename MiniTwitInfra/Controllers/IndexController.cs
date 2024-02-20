using System.Drawing;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniTwitInfra;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models;

namespace MiniTwitInfra.Controllers;

public class IndexController : PageModel
{
    private readonly int PER_PAGE = 30;
    private readonly MiniTwitContext _context;

    public IndexController(MiniTwitContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await Utility.ValidUserIsLoggedIn(HttpContext, _context))
        {
            return new RedirectToPageResult("public");
        }
        /* HttpContext.Session.SetString(Security.Authentication.AuthId, user.Id.ToString()); */
        /* HttpContext.Session.SetString(Security.Authentication.AuthuserEmail, user.Email); */

        int loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

        User loggedInUser = await MiniTwitInfra.Models.User.GetUserFromUserIdAsync(
            loggedInUserIdFromSesssion,
            _context
        );

        var followingIds = await _context
            .Followers.Where(f => f.WhoId == loggedInUser.Id)
            .Select(f => f.WhomId)
            .ToListAsync();

        var sample = await _context
            .Twits.Where(x =>
                x.AuthorId.ToString() == loggedInUser.Id.ToString()
                || followingIds.Contains(x.AuthorId)
            )
            .OrderByDescending(x => x.PubDate)
            .Take(PER_PAGE)
            .ToListAsync();

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

        ViewData["twits"] = outcome;

        if (TempData.ContainsKey("message"))
        {
            ViewData["message"] = TempData["message"];
        }

        return Page();
    }
}
