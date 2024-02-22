using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models.DataModels;


namespace MiniTwitAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MsgsController : ControllerBase
    {
        private readonly MiniTwitContext _context;

        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";

        public MsgsController(MiniTwitContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        //create an new message
        [HttpPost("{username}")]
        public async Task<ActionResult<string>> CreateMessage(string username, [FromBody] MessageData data, [FromQuery] int latest)
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            User user;
            try
            {
                user = _context.Users.Where(x => x.UserName == username).First();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Username", "User does not exist");
                return "User does not exist";
            }

            Twit newTwit = new Twit
            {
                AuthorId = user.Id,
                Text = data.Content,
                PubDate = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                Flagged = 0
            };

            await _context.Twits.AddAsync(newTwit);
            await _context.SaveChangesAsync();

            Response.ContentType = "application/json";

            return Ok(new { Message = "Your message was recorded" });
        }

        // get latest messages of the user
        [HttpGet("{username}")]
        public async Task<ActionResult<List<MessageResponse>>> GetLatestUserMessage(string username, int no, [FromQuery] int latest)
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            User user;
            try
            {
                user = _context.Users.Where(x => x.UserName == username).First();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Username", "User does not exist");
                return NotFound("User does not exist");
            }


            var latestMessages = await _context
                .Twits
                .Where(t => t.AuthorId == user.Id)
                .OrderByDescending(t => t.PubDate)
                .Take(no)
                .Join(
                    _context.Users,
                    twit => twit.AuthorId,
                    user => user.Id,
                    (twit, user) => new MessageResponse(twit.Text, user.UserName)
                )
                .ToListAsync();

            if (latestMessages == null)
            {
                return NotFound(); // Or return appropriate response if there are no messages
            }
            return Ok(latestMessages);
        }

        //get the latest messages of all messages
        [HttpGet]
        public async Task<ActionResult<List<MessageResponse>>> GetLatestMessage(int no, [FromQuery] int latest)
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            var latestMessages = await _context
                .Twits
                .OrderByDescending(t => t.PubDate)
                .Take(no)
                .Join(
                    _context.Users,
                    twit => twit.AuthorId,
                    user => user.Id,
                    (twit, user) => new MessageResponse(twit.Text, user.UserName)
                )
                .ToListAsync();

            if (latestMessages == null)
            {
                return NotFound(); // Or return appropriate response if there are no messages
            }
            return Ok(latestMessages);
        }


    }

    public class MessageData
    {
        public string Content { get; set; }
    }

    public class MessageResponse
    {
        public string Content { get; set; }
        public string User { get; set; }

        public MessageResponse(string content, string user)
        {
            this.Content = content;
            this.User = user;
        }
    }
}
