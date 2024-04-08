using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;

namespace MiniTwit.Areas.Api.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("/api/[controller]")]
    public class MsgsController : ControllerBase
    {
        private readonly MiniTwitContext _context;

        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";
        private readonly ILogger<MsgsController> _logger;

        public MsgsController(MiniTwitContext context, IMemoryCache memoryCache, ILogger<MsgsController> logger) // Added ILogger
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger; // Initialized ILogger
        }

        //create an new message
        [HttpPost("{username}")]
        public async Task<ActionResult<string>> CreateMessage(
            string username,
            [FromBody] MessageData data,
            [FromQuery] int latest
        )
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            User user;
            try
            {
                user = _context.Users.Where(x => x.UserName == username).First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message");
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
            _logger.LogInformation($"Message created by user: {username}");
            return new NoContentResult();
        }

        // get latest messages of the user
        [HttpGet("{username}")]
        public async Task<ActionResult<List<MessageResponse>>> GetLatestUserMessage(
            string username,
            int no,
            [FromQuery] int latest
        )
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            User user;
            try
            {
                user = _context.Users.Where(x => x.UserName == username).First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest user messages");
                ModelState.AddModelError("Username", "User does not exist");
                return NotFound("User does not exist");
            }

            var latestMessages = await _context
                .Twits.Where(t => t.AuthorId == user.Id)
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
                _logger.LogWarning("Warning latest message not found");
                return NotFound(); // Or return appropriate response if there are no messages
            }
            _logger.LogInformation($"Retrieved latest messages of user: {username}");
            return Ok(latestMessages);
        }

        //get the latest messages of all messages
        [HttpGet]
        public async Task<ActionResult<List<MessageResponse>>> GetLatestMessage(
            int no,
            [FromQuery] int latest
        )
        {
            _memoryCache.Set(cacheKey, latest.ToString());
            try
            {
                var latestMessages = await _context
                    .Twits.OrderByDescending(t => t.PubDate)
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
                    _logger.LogWarning("Warning latest messages of all messages not foung");
                    return NotFound();
                }
                _logger.LogInformation("Retrieved latest messages of all users");
                return Ok(latestMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest messages of all messages");
                return StatusCode(500);
            }
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
