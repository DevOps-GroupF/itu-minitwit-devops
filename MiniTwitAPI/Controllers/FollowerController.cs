using Microsoft.AspNetCore.Mvc;
using MiniTwitInfra.Models.DataModels;
using MiniTwitInfra.Data;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;



namespace MiniTwitAPI.Controllers;

[Route("/fllws")]
[ApiController]
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
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetFollow(string username, int no, int latest)
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

        var followingIds = _context
        .Followers.Where(f => f.WhoId == user.Id)
        .Select(f => f.WhomId)
        .ToList();

        var d = new Dictionary<string, List<string>>();
        var followerUsernames = _context.Users.Where(u => followingIds.Contains(u.Id)).Select(x => x.UserName).ToList();
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

        Dictionary<string, string> dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);

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

            string sqlQuery = $"INSERT INTO Follower VALUES ({user.Id}, {whom.Id})";
            await _context.Database.ExecuteSqlRawAsync(sqlQuery);

            return "successful followed person";

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

            string sqlQuery =
            $"DELETE FROM Follower WHERE who_id={user.Id} AND whom_id={whom.Id}";
            await _context.Database.ExecuteSqlRawAsync(sqlQuery);
            return "successful Unfollowed person";
        }

        Response.ContentType = "application/json";
        return "Error Eccour";
    }

}

