using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using MiniTwit.Models.DataModels;
using MiniTwit.Models.ViewModels;
using MiniTwit.Models.ViewModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [Area("FrontEnd")]
    public class HomeController : Controller
    {
        private readonly int PER_PAGE = 30;
        private readonly MiniTwitContext _context;

        public HomeController(MiniTwitContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await Utility.ValidUserIsLoggedIn(HttpContext, _context))
            {
                return RedirectToAction(actionName: "Index", controllerName: "Public");
            }
            /* HttpContext.Session.SetString(Security.Authentication.AuthId, user.Id.ToString()); */
            /* HttpContext.Session.SetString(Security.Authentication.AuthuserEmail, user.Email); */

            int loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

            User loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
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

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddMessage(IFormCollection request)
        {
            Console.WriteLine("MEssage starting to add");

            bool validUserIsLoggedIn = await Utility.ValidUserIsLoggedIn(HttpContext, _context);

            if (!validUserIsLoggedIn)
            {
                return new UnauthorizedResult();
            }

            int loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

            User loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            // Method for getting form fields
            var formCollection = Request.Form;
            string fieldText = formCollection["text"].ToString();

            if (fieldText.Length > 200)
            {
                return new BadRequestResult();
            }

            Twit newTwit =
                new()
                {
                    AuthorId = loggedInUser.Id,
                    Text = fieldText,
                    PubDate = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Flagged = 0
                };

            await _context.Twits.AddAsync(newTwit);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Your message was recorded";

            return RedirectToAction("Index");
        }
    }
}
