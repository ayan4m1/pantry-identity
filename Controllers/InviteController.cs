using Identity.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("invite")]
    public class InviteController(IdentityDbContext db) : ControllerBase
    {
        [HttpPut("create")]
        public async Task<IActionResult> Create([FromBody] string householdId, [FromBody] string email)
        {
            var guid = Guid.NewGuid();
            var household = await db.Households.FindAsync(householdId);

            if (household == null)
            {
                return BadRequest("Invalid household specified!");
            }

            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var newInvite = new Invite() {
                Email = email,
                Token = guid.ToString(),
                InvitedByUserId = userId
            };

            await db.Invites.AddAsync(newInvite);

            return Ok(guid.ToString());
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] string inviteToken)
        {
            var invite = db.Invites.Where((invite) => invite.Token == inviteToken).First();

            if (invite == null)
            {
                return BadRequest("Invalid invite token!");
            }

            db.Invites.Remove(invite);

            return Ok();
        }
    }
}
