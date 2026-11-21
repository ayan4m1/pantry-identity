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
        [HttpGet("household/{householdId:guid}/members/{userId}")]
        public async Task<ActionResult<MembershipCheckResponse>> IsHouseholdMember(Guid householdId, string userId, CancellationToken ct)
        {
            var member = await db.HouseholdMembers.Where(hm => hm.HouseholdId == householdId && hm.UserId == userId).FirstAsync(ct);

            return Ok(new MembershipCheckResponse(member != null, member?.Role));
        }
    }
}
