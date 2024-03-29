using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using MiniTwit.Models.ViewModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [Area("FrontEnd")]
    public class UserTimelineController : Controller
    {
        private readonly MiniTwitContext _context;

        private readonly int PER_PAGE = 30;

        public UserTimelineController(MiniTwitContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string username)
        {
            Console.WriteLine("UN is " + username);
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

                loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
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
            ViewData["timelineofUsername"] = pageUser.UserName;

            return View();
        }

        [HttpGet]
        [Route("{username}/Follow")]
        public async Task<IActionResult> Follow(string username)
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

            loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            if (!await Follower.DoesFollowerExistAsync(loggedInUser.Id, whomUser.Id, _context))
            {
                var newFollower = new Follower { WhoId = loggedInUser.Id, WhomId = whomUser.Id };

                var validationContext = new ValidationContext(newFollower);
                var ValidationResult = new List<ValidationResult>();

                if (
                    !Validator.TryValidateObject(
                        newFollower,
                        validationContext,
                        ValidationResult,
                        true
                    )
                )
                {
                    TempData["message"] = $"You can't follow yourself!";
                    //return BadRequest(new { Message = "Custom validation error!", Errors = ValidationResult.Select(r => r.ErrorMessage) });
                    //return BadRequest();
                }
                else
                {
                    _context.Followers.Add(newFollower);

                    try
                    {
                        await _context.SaveChangesAsync();
                        TempData["message"] = $"You are now following \"{whomUser.UserName}\"";
                    }
                    catch (DbUpdateException ex)
                    {
                        // Handle any exceptions that might occur during save changes
                        Console.WriteLine($"Error adding follower: {ex.Message}");
                    }
                }

                // Add the newFollower to the Followers DbSet
                //string sqlQuery = $"INSERT INTO Follower VALUES ({loggedInUser.Id}, {whomUser.Id})";
                //int res = await _context.Database.ExecuteSqlRawAsync(sqlQuery);
                //await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", username);
        }

        [HttpGet]
        [Route("{username}/Unfollow")]
        public async Task<IActionResult> Unfollow(string username)
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

            loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            var followerToDelete = _context.Followers.FirstOrDefault(f =>
                f.WhoId == loggedInUser.Id && f.WhomId == whomUser.Id
            );
            if (followerToDelete != null)
            {
                // Remove the follower from the context
                _context.Followers.Remove(followerToDelete);

                // Save changes to the database
                await _context.SaveChangesAsync();
                TempData["message"] = $"You are no longer following \"{whomUser.UserName}\"";
            }
            else
            {
                TempData["message"] = $"You are already not following \"{whomUser.UserName}\"";
            }

            return RedirectToAction("Index", username);
        }
    }
}
