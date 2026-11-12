using Microsoft.AspNetCore.Mvc;

namespace Identity.Controllers
{
    [ApiController]
    [Route("household")]
    public class HouseholdController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
