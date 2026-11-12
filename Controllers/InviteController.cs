using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("invite")]
    public class InviteController : Controller
    {
        [HttpPost("create")]
        public IActionResult Create([FromQuery] string householdId)
        {
            var userId = User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return NotFound();
            }

            return Ok();
        }

        [HttpDelete("delete")]
        public IActionResult Delete([FromQuery] string inviteId)
        {
            return Ok();
        }
    }
}
