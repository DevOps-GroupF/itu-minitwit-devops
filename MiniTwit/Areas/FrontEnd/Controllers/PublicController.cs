using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using MiniTwit.Models.ViewModels;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [Area("FrontEnd")]
    public class PublicController : Controller
    {
        private readonly int PER_PAGE = 30;
        private readonly MiniTwitContext _context;

        public PublicController(MiniTwitContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            Console.WriteLine("HEEEYYYYYYYY");
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

            return View();
        }
    }
}
