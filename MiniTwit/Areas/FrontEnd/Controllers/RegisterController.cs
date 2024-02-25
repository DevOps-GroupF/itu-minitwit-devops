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

        public RegisterController(MiniTwitContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            Console.WriteLine("A");
            if (!ModelState.IsValid)
            {
                return View();
            }

            Console.WriteLine("B");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.Username);
            if (user != null)
            {
                ModelState.AddModelError("Username", "The username is already taken");
                return View();
            }

            Console.WriteLine("C");
            var newUser = new User
            {
                UserName = model.Username, // Set the user properties here
                Email = model.Email,
                PasswordHash = model.Password
            };

            Console.WriteLine("D");
            string hashedPassword = _passwordHasher.HashPassword(newUser, newUser.PasswordHash);

            newUser.PasswordHash = hashedPassword;

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            Console.WriteLine("E");
            TempData["Message"] = "You were successfully registered and can login now";

            return RedirectToAction(controllerName: "Login", actionName: "Index");
        }
    }
}
