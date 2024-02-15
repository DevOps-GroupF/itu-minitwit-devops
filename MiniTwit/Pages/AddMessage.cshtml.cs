using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniTwit.Data;
using MiniTwit.Models;

namespace MiniTwit.Pages
{
    [IgnoreAntiforgeryToken]
    public class AddMessageModel : PageModel
    {
        private readonly ILogger<AddMessageModel> _logger;
        private readonly MiniTwitContext _context;

        public AddMessageModel(ILogger<AddMessageModel> logger, MiniTwitContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostAsync(IFormCollection request)
        {
            Console.WriteLine("MEssage starting to add");

            bool validUserIsLoggedIn = await Utility.ValidUserIsLoggedIn(HttpContext, _context);

            if (!validUserIsLoggedIn)
            {
                return new UnauthorizedResult();
            }

            int loggedInUserIdFromSesssion = Utility.GetUserIdFromHttpSession(HttpContext);

            User loggedInUser = await Models.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            // Method for getting form fields
            var formCollection = Request.Form;
            string fieldText = formCollection["text"].ToString();

            Models.Twit newTwit = new Twit
            {
                AuthorId = loggedInUser.Id,
                Text = fieldText,
                PubDate = (int)DateTimeOffset.Now.ToUnixTimeSeconds(),
                Flagged = 0
            };

            await _context.Twits.AddAsync(newTwit);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Your message was recorded";

            return RedirectToPage("Index");
        }
    }
}
