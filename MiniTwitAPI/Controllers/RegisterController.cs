using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniTwitInfra.Models;
using MiniTwitInfra.Data;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MiniTwitAPI.Controllers;

    [Route("[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {   

        private readonly MiniTwitContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public RegisterController(MiniTwitContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;

            Username = string.Empty;
            Pwd = string.Empty;
            Email = string.Empty;
        }
        
        [BindProperty]
        [Required(ErrorMessage = "You have to enter a username")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "You have to enter a valid email address")]
        [EmailAddress(ErrorMessage = "You have to enter a valid email address")]
        public string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "You have to enter a password")]
        public string Pwd { get; set; }


        [HttpPost]
        public async Task<ActionResult<string>> Register()
        {   
          if (!ModelState.IsValid)
            {
                return "Error";
            }

            var user = _context.Users.FirstOrDefault(u => u.UserName == Username);
            if (user != null)
            {
                ModelState.AddModelError("Username", "The username is already taken");
                return "The username is already taken";
            }

            var newUser = new User
            {
                UserName = Username, // Set the user properties here
                Email = Email,
                PasswordHash = Pwd
            };

            string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);

            newUser.PasswordHash = hashedPassword;

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return "successfull";
        }
    }

