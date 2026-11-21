using Identity.Requests;
using Identity.Responses;
using Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Controllers
{
    [ApiController]
    [Route("households/{householdId:guid}/members")]
    [Authorize(Policy = Policies.HouseholdMember)]
    public class HouseholdMembersController(IdentityDbContext db) : ControllerBase
    {
        [HttpGet]
        [EndpointName("listHouseholdMembers")]
        public async Task<ActionResult<IReadOnlyList<MemberResponse>>> List(Guid householdId, CancellationToken ct)
            => Ok(await db.HouseholdMembers.Where((hm) => hm.HouseholdId == householdId).ToListAsync(ct));

        [HttpPut("{userId}")]
        [EndpointName("updateHouseholdMemberRole")]
        [Authorize(Policy = Policies.HouseholdOwner)]   // stacks on the class-level policy
        public async Task<ActionResult<MemberResponse>> UpdateRole(
            Guid householdId, string userId, UpdateMemberRequest request, CancellationToken ct)
        {
            var member = await db.HouseholdMembers.Where(hm => hm.HouseholdId == householdId && hm.UserId == userId).FirstAsync(ct);

            if (member == null)
            {
                return NotFound();
            }

            member.Role = request.Role;

            await db.SaveChangesAsync(ct);

            return Ok(new MemberResponse(
                member.UserId,
                member.Username,
                member.Role,
                member.JoinedAt
            ));
        }

        [HttpDelete("{userId}")]
        [EndpointName("removeHouseholdMember")]
        public async Task<IActionResult> Remove(Guid householdId, string userId, CancellationToken ct)
        {
            await db.HouseholdMembers.Where((hm) => hm.HouseholdId == householdId && hm.UserId == userId).ExecuteDeleteAsync(ct);

            return NoContent();
        }
    }
}
