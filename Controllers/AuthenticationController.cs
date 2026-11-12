using Microsoft.AspNetCore.Mvc;
using pantry_identity.Entities;

namespace pantry_identity.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Username))
            {
                return Unauthorized();
            }

            return Ok(new UserIdentity()
            {
                Token = "test",
                Username = request.Username
            });
        }
    }
}
