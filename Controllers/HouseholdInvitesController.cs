using Identity.Entities;
using Identity.Requests;
using Identity.Responses;
using Identity.Services;
using Identity.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("households/{householdId:guid}/invites")]
    [Authorize(Policy = Policies.HouseholdMember)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public class HouseholdInvitesController(
        IdentityDbContext db,
        IMailService mailService,
        IOptions<FrontendSettings> frontendSettings
    ) : ControllerBase
    {
        [HttpPost]
        [EndpointName("createInvite")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public async Task<ActionResult<string>> Create(Guid householdId, [FromBody] CreateInviteRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var household = await db.Households.FindAsync([householdId], ct);

            if (household == null)
            {
                return NotFound();
            }

            var token = Guid.NewGuid();
            var newInvite = new Invite() {
                HouseholdId = household.Id,
                Email = request.Email,
                Token = token.ToString(),
                InvitedByUserId = userId,
                Status = InviteStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
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
        [ProducesResponseType<List<InviteResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<InviteResponse>>> List(Guid householdId, CancellationToken ct)
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Revoke(Guid householdId, string inviteToken, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var invite = await db.Invites.FirstOrDefaultAsync((invite) => invite.Token == inviteToken, ct);

            if (invite == null || invite.InvitedByUserId != userId || invite.HouseholdId != householdId)
            {
                return NotFound();
            }

            invite.Status = InviteStatus.Revoked;

            await db.SaveChangesAsync(ct);

            return Ok();
        }
    }
}
