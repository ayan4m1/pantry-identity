using Identity.Entities;
using Identity.Requests;
using Identity.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class HouseholdsController(
        IdentityDbContext db,
        IInventoryClient inventoryClient,
        ILogger<HouseholdsController> logger
    ) : ControllerBase
    {
        [HttpGet]
        [EndpointName("listHouseholds")]
        [ProducesResponseType<List<HouseholdResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<HouseholdResponse>>> List(CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            return Ok(await db.Households
                .Where((household) => household.Members.Any((member) => member.UserId == userId))
                .Select((household) => new HouseholdResponse(household.Id, household.Name, household.CreatedAt, household.Members.Count))
                .ToListAsync(ct));
        }

        [HttpPost]
        [EndpointName("createHousehold")]
        [ProducesResponseType<HouseholdResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<HouseholdResponse>> Create([FromBody] CreateHouseholdRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var household = new Household()
            {
                Name = request.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            household.Members.Add(new HouseholdMember()
            {
                UserId = userId,
                Username = User.FindFirstValue("preferred_username") ?? "Unknown",
                Role = HouseholdRole.Owner,
                JoinedAt = DateTime.UtcNow
            });

            var result = await db.Households.AddAsync(household, ct);

            await db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(Create), new { householdId = result.Entity.Id }, new HouseholdResponse(
                result.Entity.Id,
                result.Entity.Name,
                result.Entity.CreatedAt,
                result.Entity.Members.Count
            ));
        }

        [HttpGet("{id:guid}")]
        [EndpointName("getHousehold")]
        [ProducesResponseType<HouseholdDetailResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HouseholdDetailResponse>> Get(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var household = await db.Households.Include(h => h.Members).FirstOrDefaultAsync(h => h.Id == id, ct);

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            // ensure the current user is in this household
            if (household.Members.Find((hm) => hm.UserId == userId) == null)
            {
                return NotFound();
            }

            return Ok(new HouseholdDetailResponse(
                household.Id,
                household.Name,
                household.CreatedAt,
                [.. household.Members.Select((hm) => new MemberResponse(hm.UserId, hm.Username, hm.Role, hm.JoinedAt))]
             ));
        }

        [HttpPut("{id:guid}")]
        [EndpointName("updateHousehold")]
        [ProducesResponseType<HouseholdDetailResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<HouseholdDetailResponse>> Update(Guid id, [FromBody] UpdateHouseholdRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var household = await db.Households.Include(h => h.Members).FirstOrDefaultAsync(h => h.Id == id, ct);

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            // ensure the current user is in this household
            if (household.Members.Find((hm) => hm.UserId == userId) == null)
            {
                return NotFound();
            }

            household.Name = request.Name;

            await db.SaveChangesAsync(ct);

            return Ok(new HouseholdDetailResponse(
                household.Id,
                household.Name,
                household.CreatedAt,
                [.. household.Members.Select((hm) => new MemberResponse(hm.UserId, hm.Username, hm.Role, hm.JoinedAt))]
            ));
        }

        [HttpDelete]
        [EndpointName("deleteHousehold")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromQuery] Guid householdId, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub")!;

            var household = await db.Households.FindAsync([householdId], ct);

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            var member = await db.HouseholdMembers.FindAsync([householdId, userId], ct);

            // only allow household owner to delete it
            if (member?.Role != HouseholdRole.Owner)
            {
                return Forbid();
            }

            db.Households.Remove(household);

            await db.SaveChangesAsync(ct);

            // Best-effort cleanup of stock rows that referenced this household. If this fails the
            // rows are orphaned; a transactional outbox would make this delivery guaranteed.
            try
            {
                await inventoryClient.DeleteHouseholdStockAsync(householdId, ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to delete inventory/shopping stock for household {HouseholdId}", householdId);
            }

            return NoContent();
        }
    }
}
