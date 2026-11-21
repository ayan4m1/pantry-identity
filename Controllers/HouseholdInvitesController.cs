using Identity.Entities;
using Identity.Requests;
using Identity.Responses;
using Identity.Services;
using Identity.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("households/{householdId:guid}/invites")]
    public class HouseholdInvitesController(
        IdentityDbContext db,
        IMailService mailService,
        IOptions<FrontendSettings> frontendSettings
    ) : ControllerBase
    {
        [HttpPost]
        [EndpointName("createInvite")]
        public async Task<ActionResult<string>> Create(Guid householdId, [FromBody] CreateInviteRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households
                .Include((household) => household.Members)
                .FirstOrDefaultAsync((household) => household.Id == householdId, ct);

            if (household == null)
            {
                return BadRequest("Invalid household specified!");
            }

            var member = household.Members.Find((member) => member.UserId == userId);

            if (member == null)
            {
                return Unauthorized();
            }

            var token = Guid.NewGuid();
            var newInvite = new Invite() {
                HouseholdId = household.Id,
                Email = request.Email,
                Token = token.ToString(),
                InvitedByUserId = userId,
                Status = InviteStatus.Pending,
                Role = request.Role
            };

            await db.Invites.AddAsync(newInvite, ct);

            await db.SaveChangesAsync(ct);

            await mailService.SendEmailAsync(
                request.Email,
                $"You are invited to {household.Name}!",
                $"Click the following link to complete the invite process: {frontendSettings.Value.BaseUrl}/invite/{token}"
            );

            return Ok(token.ToString());
        }

        [HttpGet]
        [EndpointName("listInvites")]
        public async Task<ActionResult<InviteResponse>> List(Guid householdId, CancellationToken ct)
        {
            return Ok(await db.Invites
                .Where((r) => r.HouseholdId == householdId)
                .Select((i) => new InviteResponse(
                    i.Id,
                    i.HouseholdId,
                    i.Email,
                    i.Status,
                    i.InvitedByUserId,
                    i.CreatedAt,
                    i.ExpiresAt
                )).ToListAsync(ct)
            );
        }

        [HttpDelete("revoke/{inviteToken}")]
        [EndpointName("revokeInvite")]
        public async Task<IActionResult> Revoke(string inviteToken, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var invite = await db.Invites.FirstOrDefaultAsync((invite) => invite.Token == inviteToken, ct);

            if (invite == null || invite.InvitedByUserId != userId)
            {
                return NotFound();
            }

            invite.Status = InviteStatus.Revoked;

            await db.SaveChangesAsync(ct);

            return Ok();
        }
    }
}
