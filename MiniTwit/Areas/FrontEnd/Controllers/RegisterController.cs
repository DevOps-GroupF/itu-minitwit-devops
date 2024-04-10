using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using MiniTwit.Models.ViewModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [IgnoreAntiforgeryToken]
    [Area("FrontEnd")]
    public class RegisterController : Controller
    {
        private readonly MiniTwitContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(MiniTwitContext context, IPasswordHasher<User> passwordHasher, ILogger<RegisterController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        [Route("/ui-register")]
        public IActionResult Index()
        {
            try
            {
                _logger.LogInformation("GET request received for Index action");
                return View(new RegisterViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing GET request for Index action");
                throw;
            }
        }
        
        [HttpPost]
        [Route("/ui-register")]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            try
            {
                _logger.LogInformation($"POST request received for Index action by user {model.Username}, tries to register");
                if (!ModelState.IsValid)
                {
                    return View();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.Username);
                if (user != null)
                {
                    ModelState.AddModelError("Username", "The username is already taken");
                    return View();
                }

                var newUser = new User
                {
                    UserName = model.Username, // Set the user properties here
                    Email = model.Email,
                    PasswordHash = model.Pwd
                };

                
                string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);

                newUser.PasswordHash = hashedPassword;

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

                Console.WriteLine("E");
                TempData["Message"] = "You were successfully registered and can login now";
                _logger.LogInformation($"User {model.Username} registered successfully");
                return RedirectToAction(controllerName: "Login", actionName: "Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while processing POST request for Index action by user {model.Username}");
                throw;
            }
        }

        [HttpPost]
        [Route("/register")]
        public async Task<string> register([FromBody] SimulatorRegisterViewModel model)
        {       
            try
            {
                _logger.LogInformation($"POST request received for Index action by user {model.username}, tries to register");
                if (!ModelState.IsValid)
                {   

                     var message = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                    return message;
                }


                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.username);
                if (user != null)
                {
                    ModelState.AddModelError("Username", "The username is already taken");
                    return "username already taken";
                }

                var newUser = new User
                {
                    UserName = model.username, // Set the user properties here
                    Email = model.email,
                    PasswordHash = model.pwd
                };

                
                string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);

                newUser.PasswordHash = hashedPassword;

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();

              
                TempData["Message"] = "You were successfully registered and can login now";
                _logger.LogInformation($"User {model.username} registered successfully");
                return "success";
                //return RedirectToAction(controllerName: "Login", actionName: "Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while processing POST request for Index action by user {model.username}");
                throw;
            }
        }
    }
    }

