using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using Newtonsoft.Json;

namespace MiniTwit.Areas.Api.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("/api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly MiniTwitContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(
            MiniTwitContext context,
            IPasswordHasher<User> passwordHasher,
            IMemoryCache memoryCache,
            ILogger<RegisterController> logger
        )
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _memoryCache = memoryCache;
            _logger = logger;

            /*
            username = string.Empty;
            pwd = string.Empty;
            email = string.Empty;
            */
        }

        [HttpPost]
        public async Task<ActionResult<string>> Register(int latest)
        {
            _memoryCache.Set(cacheKey, latest.ToString());

            try
            {
                string body;
                using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
                {
                    body = await stream.ReadToEndAsync();
                }

                Dictionary<string, string> dataDic = JsonConvert.DeserializeObject<
                    Dictionary<string, string>
                >(body);

                string username = dataDic["username"].ToString();
                string email = dataDic["email"].ToString();
                string pwd = dataDic["pwd"].ToString();

                var user = _context.Users.FirstOrDefault(u => u.UserName == username);
                if (user != null)
                {
                    _logger.LogWarning($"Register: User registration failed. Username '{username}' is already taken.");
                    return StatusCode(403, "The username is already taken"); // works!
                }

                var newUser = new User
                {
                    UserName = username, // Set the user properties here
                    Email = email,
                    PasswordHash = pwd
                };

                string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);
                newUser.PasswordHash = hashedPassword;

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                Response.ContentType = "application/json";
                _logger.LogInformation($"Register: User '{username}' registered successfully.");
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(500); // Or return appropriate response for server error
            }
        }

        /*
        [HttpPost]
        public async Task<ActionResult<string>> Register()
        {
          if (!ModelState.IsValid)
            {
                return "Error";
            }
    
            var user = _context.Users.FirstOrDefault(u => u.UserName == username);
            if (user != null)
            {
                ModelState.AddModelError("Username", "The username is already taken");
                return "The username is already taken";
            }
    
            var newUser = new User
            {
                UserName = username, // Set the user properties here
                Email = email,
                PasswordHash = pwd
            };
    
            string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);
    
            newUser.PasswordHash = hashedPassword;
    
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
    
            return "successfull";
        }
    */
    }
}
