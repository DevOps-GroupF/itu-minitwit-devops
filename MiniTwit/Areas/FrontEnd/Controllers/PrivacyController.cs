using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MiniTwit.Areas.FrontEnd.Controllers
{
    [Area("FrontEnd")]
    public class PrivacyController : Controller
    {
        private readonly ILogger<PrivacyController> _logger;

        public PrivacyController(ILogger<PrivacyController> logger)
        {
            _logger = logger;
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
