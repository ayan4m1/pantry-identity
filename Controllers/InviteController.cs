using Identity.Entities;
using Identity.Requests;
using Identity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [EndpointName("createInvite")]
        public async Task<ActionResult<string>> Create([FromBody] InviteCreateRequest request)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households
                .Include((household) => household.Members)
                .FirstOrDefaultAsync((household) => household.Id == request.HouseholdId);

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
                HouseholdId = household.Id,
                Email = request.Email,
                Token = guid.ToString(),
                InvitedByUserId = userId,
                Status = InviteStatus.Pending,
                Role = request.Role
            };

            await db.Invites.AddAsync(newInvite);

            await db.SaveChangesAsync();

            await mailService.SendEmailAsync(
                request.Email,
                $"You are invited to {household.Name}!",
                $"Click the following link to complete the invite process: {frontendSettings.Value.BaseUrl}/invite/{guid}"
            );

            return Ok(guid.ToString());
        }

        [HttpPut("register")]
        [EndpointName("registerInvite")]
        public async Task<IActionResult> Register([FromQuery] string inviteToken)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var invite = await db.Invites
                .Include((invite) => invite.Household)
                .FirstOrDefaultAsync((invite) => invite.Token == inviteToken);

            if (invite == null)
            {
                return BadRequest("Invalid invite token!");
            }

            invite.Status = InviteStatus.Accepted;

            invite.Household.Members.Add(new HouseholdMember() {
                UserId = userId,
                Role = invite.Role
            });

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("delete")]
        [EndpointName("deleteInvite")]
        public async Task<IActionResult> Delete([FromQuery] string inviteToken)
        {
            var invite = await db.Invites.FirstOrDefaultAsync((invite) => invite.Token == inviteToken);

            if (invite == null)
            {
                return BadRequest("Invalid invite token!");
            }

            db.Invites.Remove(invite);

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
