using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using Newtonsoft.Json;

namespace MiniTwit.Areas.Api.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("/api/fllws")]
    public class FollowController : ControllerBase
    {
        private readonly MiniTwitContext _context;
        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";

        private readonly ILogger<FollowController> _logger;

        public FollowController(ILogger<FollowController> logger, MiniTwitContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// Used to get a given no. of follow for the a given user
        /// </summary>
        /// <param name="no"></param>
        /// <param name="latest"></param>
        /// <returns></returns>
        [HttpGet("{username}")]
        public async Task<ActionResult<Dictionary<string, List<string>>>> GetFollow(
            string username,
            int no,
            int latest
        )
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            User user;
            try
            {
                user = _context.Users.FirstOrDefault(u => u.UserName == username);
            }
            catch (ArgumentException e)
            {
                _logger.LogError(e, "GetFollow: Error occurred while getting followers");
                throw new ArgumentException(e.Message);
            }

            if (user == null)
            {
                _logger.LogWarning("User not found");
                return NotFound();
            }

            _logger.LogInformation($"Follow: Try to get followers of {username}");

            var followingIds = _context
                .Followers.Where(f => f.WhoId == user.Id)
                .Select(f => f.WhomId)
                .ToList();

            var d = new Dictionary<string, List<string>>();
            var followerUsernames = _context
                .Users.Where(u => followingIds.Contains(u.Id))
                .Select(x => x.UserName)
                .ToList();
            d.Add("follows", followerUsernames);

            _logger.LogInformation($"GetFollow: Get followers of {username} successful");
            Response.ContentType = "application/json";
            return d;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult<string>> FollowAction(string username, int latest)
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            string body;
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                body = await stream.ReadToEndAsync();
            }

            Dictionary<string, string> dataDic = JsonConvert.DeserializeObject<
                Dictionary<string, string>
            >(body);

            Response.ContentType = "application/json";

            if (dataDic.ContainsKey("follow"))
            {
                // followed person
                User user;
                User whom;
                try
                {
                    user = _context.Users.FirstOrDefault(u => u.UserName == username);
                    whom = _context.Users.FirstOrDefault(u => u.UserName == dataDic["follow"]);
                }
                catch (ArgumentException e)
                {
                    _logger.LogError(e, "Error occurred while processing follow request");
                    throw new ArgumentException(e.Message);
                }

                if (user != null && whom != null)
                {
                    var newFollower = new Follower
                    {
                        WhoId = user.Id,
                        WhomId = whom.Id
                    };

                    _logger.LogInformation($"Follow: {user.Id} tries to follow {whom.Id}");

                    var validationContext = new ValidationContext(newFollower);
                    var ValidationResult = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(newFollower, validationContext, ValidationResult, true))
                    {
                        _logger.LogWarning("Follow: You can't follow yourself");
                        return BadRequest("You can not follow yourself");
                    }
                    else
                    {
                        _context.Followers.Add(newFollower);
                        try
                        {
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Follow: {user.Id} follows {whom.Id}");
                            return new NoContentResult();

                        }
                        catch (DbUpdateException ex)
                        {
                            // Handle any exceptions that might occur during save changes
                            _logger.LogError(ex, "Follow: Error occurred while following user");
                            Console.WriteLine($"Error adding follower: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Follow: User not found");
                    return NotFound("User not found");
                }

                return new NoContentResult();

            }
            else if (dataDic.ContainsKey("unfollow"))
            {
                // unfollow person
                User user;
                User whom;
                try
                {
                    user = _context.Users.FirstOrDefault(u => u.UserName == username);
                    whom = _context.Users.FirstOrDefault(u => u.UserName == dataDic["unfollow"]);
                }
                catch (ArgumentException e)
                {
                    _logger.LogError(e, "Unfollow: Error while unfollowing user");
                    throw new ArgumentException(e.Message);
                }

                if (user != null && whom != null)
                {
                    _logger.LogInformation($"Unfollow: {user.Id} tries to unfollow {whom.Id}");
                    var followerToDelete = _context.Followers.FirstOrDefault(f => f.WhoId == user.Id && f.WhomId == whom.Id);
                    if (followerToDelete != null)
                    {
                        // Remove the follower from the context
                        _context.Followers.Remove(followerToDelete);

                        // Save changes to the database
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Unfollow: {user} unfollowed {whom}");
                        return new NoContentResult();
                    }
                    else
                    {
                        _logger.LogWarning("Unfollow: You are already not following the user");
                        return BadRequest("You are already not following the user");
                    }
                }
                else
                {
                    _logger.LogWarning("Unfollow: Follower user not found");
                    return NotFound("Follower user not found");
                }
            }

            Response.ContentType = "application/json";
            _logger.LogError("Unfollow: Error occured");
            return "Error Eccour";
        }
    }
}
