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
        private readonly ILogger<UserTimelineController> _logger;

        public UserTimelineController(MiniTwitContext context, ILogger<UserTimelineController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string username)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while processing GET request for Index action by user");
                throw;
            }
        }

        [HttpGet]
        [Route("{username}/Follow")]
        public async Task<IActionResult> Follow(string username)
        {
            try
            {
                User whomUser;
                _logger.LogInformation($"GET request received for Follow action by user with username: {username}");
                whomUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (whomUser == null)
                {
                    _logger.LogWarning($"Whom user {whomUser} not found");
                    return new NotFoundResult();
                }

                bool validUserIsLoggedIn = await Utility.ValidUserIsLoggedIn(HttpContext, _context);

                if (!validUserIsLoggedIn)
                {
                    _logger.LogWarning("Unauthorized request received for Follow action");
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
                        _logger.LogWarning($"User with username {loggedInUser.UserName} attempted to follow themselves");
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
                            _logger.LogInformation($"User with username {loggedInUser.UserName} followed user with username {whomUser.UserName}");
                        }
                        catch (DbUpdateException ex)
                        {
                            // Handle any exceptions that might occur during save changes
                            _logger.LogError(ex, $"Error occurred while following user with username: {whomUser.UserName}");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while processing GET request for Follow action by user with username: {username}");
                throw;
            }
        }

        [HttpGet]
        [Route("{username}/Unfollow")]
        public async Task<IActionResult> Unfollow(string username)
        {
            try
            {

                User whomUser;
                _logger.LogInformation($"GET request received for Unfollow action by user with username: {username}");

                whomUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (whomUser == null)
                {
                    _logger.LogWarning($"whomuser {whomUser} not found");
                    return new NotFoundResult();
                }

                if (!await Utility.ValidUserIsLoggedIn(HttpContext, _context))
                {
                    _logger.LogWarning("Unauthorized request received for Unfollow action");
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
                    _logger.LogInformation($"User with username {loggedInUser.UserName} unfollowed user with username {whomUser.UserName}");
                }
                else
                {
                    TempData["message"] = $"You are already not following \"{whomUser.UserName}\"";
                    _logger.LogWarning($"User with username {loggedInUser.UserName} attempted to unfollow user with username {whomUser.UserName} whom they were not following");
                }

                return RedirectToAction("Index", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while processing GET request for Unfollow action by user with username: {username}");
                throw;
            }
        }
    }
}
