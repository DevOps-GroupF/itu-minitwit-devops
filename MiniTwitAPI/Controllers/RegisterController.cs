using Microsoft.AspNetCore.Mvc;
using MiniTwitInfra.Models.DataModels;
using MiniTwitInfra.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;



namespace MiniTwitAPI.Controllers;

    [Route("[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {   
        private readonly MiniTwitContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IMemoryCache _memoryCache;
        public string cacheKey = "latest";

        public RegisterController(MiniTwitContext context, IPasswordHasher<User> passwordHasher, IMemoryCache memoryCache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _memoryCache = memoryCache;

            /*
            username = string.Empty;
            pwd = string.Empty;
            email = string.Empty;
            */
        }
        

         [HttpPost]
        public async Task<ActionResult<string>> Register(int latest)
        {       
            //ModelState.Clear();

            _memoryCache.Set(cacheKey, latest.ToString());
    
            string body; 
            using (StreamReader stream = new StreamReader(HttpContext.Request.Body))
            {
                body =  await stream.ReadToEndAsync();
            }

            Dictionary<string, string> dataDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
            
            string username = dataDic["username"].ToString();
            string email = dataDic["email"].ToString();
            string pwd = dataDic["pwd"].ToString();
            
             var user = _context.Users.FirstOrDefault(u => u.UserName == username);
            if (user != null)
            {
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

            Dictionary<string, string> respose = new Dictionary<string, string>();
            respose.Add("latest", latest.ToString());
            
            Response.ContentType = "application/json";
            return "successful created user";
              
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

