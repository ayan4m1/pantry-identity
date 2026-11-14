using Microsoft.AspNetCore.Mvc;

namespace Identity.Controllers
{
    [ApiController]
    [Route("household")]
    public class HouseholdController : ControllerBase
    {
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
