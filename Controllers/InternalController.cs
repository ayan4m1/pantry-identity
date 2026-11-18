using Identity.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.Controllers
{
    // Service-to-service endpoints; the gateway blocks /api/{service}/internal/** from the outside.
    [ApiController]
    [Route("internal")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class InternalController(IdentityDbContext db) : ControllerBase
    {
        [HttpGet("household/{householdId:guid}/member/{userId}")]
        public async Task<ActionResult<HouseholdRole>> GetMemberRole(Guid householdId, string userId)
        {
            var member = await db.HouseholdMembers.FindAsync(householdId, userId);

            if (member == null)
            {
                return NotFound();
            }

            return Ok(member.Role);
        }
    }
}
