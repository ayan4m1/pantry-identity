using Identity.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Controllers
{
    // Service-to-service endpoints; the gateway blocks /api/{service}/internal/** from the outside.
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalController(IdentityDbContext db) : ControllerBase
    {
        [HttpGet("household/{householdId:guid}/member/{userId}")]
        public async Task<ActionResult<MembershipCheckResponse>> IsHouseholdMember(Guid householdId, string userId, CancellationToken ct)
        {
            var member = await db.HouseholdMembers.Where(hm => hm.HouseholdId == householdId && hm.UserId == userId).FirstOrDefaultAsync(ct);

            if (member == null)
            {
                return NotFound();
            }

            return Ok(new MembershipCheckResponse(true, member.Role));
        }
    }
}
