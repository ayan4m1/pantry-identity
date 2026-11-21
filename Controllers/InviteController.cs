using Identity.Entities;
using Identity.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("invites")]
    public class InviteController(IdentityDbContext db) : ControllerBase
    {
        [HttpGet("{token}")]
        [EndpointName("getInvitePreview")]
        public async Task<ActionResult<InvitePreviewResponse>> GetPreview(string token)
        {
            var userId = User.FindFirstValue("sub");

            var invite = await db.Invites.Where((i) => i.Token == token).FirstAsync();

            if (invite == null || invite.Status != InviteStatus.Pending)
            {
                return NotFound();
            }

            return Ok(invite);
        }

        [HttpPost("{token}/accept")]
        public async Task<ActionResult<MyHouseholdResponse>> Accept(string token)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var invite = await db.Invites
                .Include((invite) => invite.Household)
                .FirstOrDefaultAsync((invite) => invite.Token == token);

            if (invite == null)
            {
                return BadRequest("Invalid invite token!");
            }

            // todo: get user email and verify it matches

            invite.Status = InviteStatus.Accepted;
            invite.Household.Members.Add(new HouseholdMember()
            {
                UserId = userId,
                Username = User.FindFirstValue("preferred_username") ?? "Unknown",
                Role = invite.Role
            });

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
