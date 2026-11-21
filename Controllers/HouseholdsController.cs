using Identity.Entities;
using Identity.Requests;
using Identity.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("household")]
    public class HouseholdsController(
        IdentityDbContext db,
        IInventoryClient inventoryClient,
        ILogger<HouseholdsController> logger
    ) : ControllerBase
    {
        [HttpGet]
        [EndpointName("listHouseholds")]
        public async Task<ActionResult<IReadOnlyList<HouseholdResponse>>> List(CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

            return Ok(await db.Households
                .Where((household) => household.Members.Any((member) => member.UserId == userId))
                .Select((household) => new HouseholdResponse(household.Id, household.Name, household.CreatedAt, household.Members.Count))
                .ToListAsync(ct));
        }

        [HttpPost]
        [EndpointName("createHousehold")]
        public async Task<ActionResult<HouseholdResponse>> Create([FromBody] CreateHouseholdRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (string.IsNullOrWhiteSpace(userId))
            {
                return NotFound();
            }

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
        public async Task<ActionResult<HouseholdDetailResponse>> Get(Guid id, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

#pragma warning disable CA2016
            var household = await db.Households.FindAsync(id, ct);
#pragma warning restore CA2016

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
                [.. household.Members.Select((hm) => new MemberResponse(hm.UserId, hm.Role, hm.JoinedAt))]
             ));
        }

        [HttpPut("{id:guid}")]
        [EndpointName("updateHousehold")]
        public async Task<ActionResult<HouseholdDetailResponse>> Update(Guid id, [FromBody] UpdateHouseholdRequest request, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

#pragma warning disable CA2016
            var household = await db.Households.FindAsync(id, ct);
#pragma warning restore CA2016

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            household.Name = request.Name;

            await db.SaveChangesAsync(ct);

            return Ok(new HouseholdDetailResponse(
                household.Id,
                household.Name,
                household.CreatedAt,
                [.. household.Members.Select((hm) => new MemberResponse(hm.UserId, hm.Role, hm.JoinedAt))]
            ));
        }

        [HttpDelete]
        [EndpointName("deleteHousehold")]
        public async Task<IActionResult> Delete([FromQuery] Guid householdId, CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

#pragma warning disable CA2016
            var household = await db.Households.FindAsync(householdId, ct);
#pragma warning restore CA2016

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

#pragma warning disable CA2016
            var member = await db.HouseholdMembers.FindAsync(householdId, userId, ct);
#pragma warning restore CA2016

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
