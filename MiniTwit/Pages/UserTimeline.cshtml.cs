using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models;

namespace MiniTwit.Pages;

public class UserTimelineModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly MiniTwitContext _context;

    private readonly int PER_PAGE = 30;

    public UserTimelineModel(ILogger<IndexModel> logger, MiniTwitContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> OnGet(string username)
    {
        var tempData = TempData["message"];
        if (tempData != null)
        {
            ViewData["message"] = tempData.ToString();
        }

        User pageUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (pageUser == null)
        {
            return new NotFoundResult();
        }

        if (await Utility.ValidUserIsLoggedIn(HttpContext, _context))
        {
            int loggedInUserIdFromSesssion;
            User loggedInUser;

            loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

            loggedInUser = await MiniTwitInfra.Models.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            ViewData["user"] = loggedInUser.Id;

            bool followed = _context.Followers.Any(f =>
                f.WhoId == loggedInUser.Id && f.WhomId == pageUser.Id
            );
            ViewData["followed"] = followed;
        }

        var messagesWithUsers = await _context
            .Twits.Where(t => t.AuthorId == pageUser.Id)
            .OrderByDescending(t => t.PubDate)
            .Take(PER_PAGE)
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
            .ToListAsync();

        ViewData["twits"] = messagesWithUsers;
        ViewData["timelineof"] = pageUser.Id;

        return Page();
    }

    public async Task<IActionResult> OnGetFollow(string username)
    {
        User whomUser;

        whomUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (whomUser == null)
        {
            return new NotFoundResult();
        }

        bool validUserIsLoggedIn = await Utility.ValidUserIsLoggedIn(HttpContext, _context);

        if (!validUserIsLoggedIn)
        {
            return new UnauthorizedResult();
        }

        int loggedInUserIdFromSesssion;
        User loggedInUser;

        loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

        loggedInUser = await MiniTwitInfra.Models.User.GetUserFromUserIdAsync(
            loggedInUserIdFromSesssion,
            _context
        );

        if (!await Follower.DoesFollowerExistAsync(loggedInUser.Id, whomUser.Id, _context))
        {
            string sqlQuery = $"INSERT INTO Follower VALUES ({loggedInUser.Id}, {whomUser.Id})";
            await _context.Database.ExecuteSqlRawAsync(sqlQuery);
        }

        TempData["message"] = $"You are now following \"{whomUser.UserName}\"";

        return await OnGet(username);
    }

    public async Task<IActionResult> OnGetUnfollow(string username)
    {
        User whomUser;

        whomUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (whomUser == null)
        {
            return new NotFoundResult();
        }

        if (!await Utility.ValidUserIsLoggedIn(HttpContext, _context))
        {
            return new UnauthorizedResult();
        }

        int loggedInUserIdFromSesssion;
        User loggedInUser;

        loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

        loggedInUser = await MiniTwitInfra.Models.User.GetUserFromUserIdAsync(
            loggedInUserIdFromSesssion,
            _context
        );

        if (await Follower.DoesFollowerExistAsync(loggedInUser.Id, whomUser.Id, _context))
        {
            string sqlQuery =
                $"DELETE FROM Follower WHERE who_id={loggedInUser.Id} AND whom_id={whomUser.Id}";

            await _context.Database.ExecuteSqlRawAsync(sqlQuery);
        }

        TempData["message"] = $"You are no longer following \"{whomUser.UserName}\"";

        return await OnGet(username);
    }
}
