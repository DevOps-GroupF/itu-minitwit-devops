using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MiniTwitInfra.Data;
using MiniTwitInfra.Models.ViewModels;
using MiniTwitInfra.Models.DataModels;

namespace MiniTwitInfra.Controllers
{

    public class PublicController : PageModel
    {
        private readonly int PER_PAGE = 30;
        private readonly MiniTwitContext _context;

        public PublicController(MiniTwitContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            var sample = _context.Twits.OrderByDescending(x => x.PubDate).Take(PER_PAGE).ToList();

            var outcome = sample
                .Join(
                    _context.Users,
                    message => message.AuthorId,
                    user => user.Id,
                    (message, user) =>
                        new TwitViewModel
                        {
                            AuthorUsername = user.UserName,
                            Text = message.Text,
                            PubDate = message.PubDate,
                            GravatarString = Utility.GetGravatar(user.Email, 48)
                        }
                )
                .ToList();

            if (TempData.ContainsKey("message"))
            {
                ViewData["message"] = TempData["message"];
            }
            ViewData["twits"] = outcome;

            return Page();
        }
    }
}