using Identity.Entities;
using Identity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("invite")]
    public class InviteController(
        IdentityDbContext db,
        IMailService mailService,
        IOptions<FrontendSettings> frontendSettings
    ) : ControllerBase
    {
        [HttpPut("create")]
        public async Task<IActionResult> Create([FromBody] string householdId, [FromBody] string email)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households.FindAsync(householdId);

            if (household == null)
            {
                return BadRequest("Invalid household specified!");
            }

            var member = household.Members.Find((member) => member.UserId == userId);

            if (member == null)
            {
                return Unauthorized();
            }

            var guid = Guid.NewGuid();
            var newInvite = new Invite() {
                Email = email,
                Token = guid.ToString(),
                InvitedByUserId = userId
            };

            await db.Invites.AddAsync(newInvite);

            await mailService.SendEmailAsync(
                email,
                $"You are invited to {household.Name}!",
                $"Click the following link to complete the invite process: {frontendSettings.Value.Hostname}/invite/{guid}"
            );

            return Ok(guid.ToString());
        }

        [HttpPut("register")]
        public async Task<IActionResult> Register([FromQuery] string inviteToken)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var invite = db.Invites.Where((invite) => invite.Token == inviteToken).First();

            if (invite == null)
            {
                return BadRequest("Invalid invite token!");
            }

            invite.Household.Members.Add(new HouseholdMember() {
                UserId = userId,
                Role = HouseholdRole.Member
            });

            return Ok();
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
