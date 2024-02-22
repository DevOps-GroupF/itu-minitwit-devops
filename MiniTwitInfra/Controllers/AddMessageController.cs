using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models.DataModels;

namespace MiniTwitInfra.Controllers
{
    [IgnoreAntiforgeryToken]
    public class AddMessageController : PageModel
    {
        private readonly MiniTwitContext _context;

        public AddMessageController(MiniTwitContext context)
        {
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

            User loggedInUser = await Models.DataModels.User.GetUserFromUserIdAsync(
                loggedInUserIdFromSesssion,
                _context
            );

            // Method for getting form fields
            var formCollection = Request.Form;
            string fieldText = formCollection["text"].ToString();

            if (fieldText.Length > 200)
            {
                return new BadRequestResult();
            }

            Twit newTwit =
                new()
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
