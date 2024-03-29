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

        public FollowController(MiniTwitContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
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
                throw new ArgumentException(e.Message);
            }

            if (user == null)
            {
                return NotFound();
            }

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
                    throw new ArgumentException(e.Message);
                }

                if (user != null && whom != null)
                {
                    var newFollower = new Follower
                    {
                        WhoId = user.Id,
                        WhomId = whom.Id
                    };

                    var validationContext = new ValidationContext(newFollower);
                    var ValidationResult = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(newFollower, validationContext, ValidationResult, true))
                    {
                        return BadRequest("You can not follow yourself");
                    }
                    else
                    {
                        _context.Followers.Add(newFollower);
                        try
                        {
                            await _context.SaveChangesAsync();
                            return new NoContentResult();

                        }
                        catch (DbUpdateException ex)
                        {
                            // Handle any exceptions that might occur during save changes
                            Console.WriteLine($"Error adding follower: {ex.Message}");
                        }
                    }
                }
                else
                {
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
                    throw new ArgumentException(e.Message);
                }

                if (user != null && whom != null)
                {
                    var followerToDelete = _context.Followers.FirstOrDefault(f => f.WhoId == user.Id && f.WhomId == whom.Id);
                    if (followerToDelete != null)
                    {
                        // Remove the follower from the context
                        _context.Followers.Remove(followerToDelete);

                        // Save changes to the database
                        await _context.SaveChangesAsync();
                        return new NoContentResult();
                    }
                    else
                    {
                        return BadRequest("You are already not following the user");
                    }
                }
                else
                {
                    return NotFound("Follower user not found");
                }
            }

            Response.ContentType = "application/json";
            return "Error Eccour";
        }
    }
}
