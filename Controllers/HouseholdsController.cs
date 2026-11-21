using Identity.Entities;
using Identity.Requests;
using Identity.Responses;
using Identity.Services;
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
        IShoppingClient shoppingClient,
        ILogger<HouseholdsController> logger
    ) : ControllerBase
    {
        [HttpGet]
        [EndpointName("listHouseholds")]
        public async Task<ActionResult<IReadOnlyList<HouseholdResponse>>> List()
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
                .ToListAsync());
        }

        [HttpPost]
        [EndpointName("createHousehold")]
        public async Task<ActionResult<HouseholdResponse>> Create([FromBody] CreateHouseholdRequest request)
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
                CreatedAt = DateTime.Now,
                CreatedByUserId = userId
            };

            household.Members.Add(new HouseholdMember()
            {
                UserId = userId,
                Role = HouseholdRole.Owner,
                JoinedAt = DateTime.Now
            });

            var result = await db.Households.AddAsync(household);

            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(Create), new { householdId = result.Entity.Id }, new HouseholdResponse(
                result.Entity.Id,
                result.Entity.Name,
                result.Entity.CreatedAt,
                result.Entity.Members.Count
            ));
        }

        [HttpGet("{id:guid}")]
        [EndpointName("getHousehold")]
        public async Task<ActionResult<HouseholdDetailResponse>> Get(Guid id)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households.FindAsync(id);

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
        public async Task<ActionResult<HouseholdDetailResponse>> Update(Guid id, [FromBody] UpdateHouseholdRequest request)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households.FindAsync(id);

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            household.Name = request.Name;

            await db.SaveChangesAsync();

            return Ok(new HouseholdDetailResponse(
                household.Id,
                household.Name,
                household.CreatedAt,
                [.. household.Members.Select((hm) => new MemberResponse(hm.UserId, hm.Role, hm.JoinedAt))]
            ));
        }

        [HttpDelete]
        [EndpointName("deleteHousehold")]
        public async Task<IActionResult> Delete([FromQuery] Guid householdId)
        {
            var userId = User.FindFirstValue("sub");

            // ensure the user ID is present
            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households.FindAsync(householdId);

            // ensure the household ID is valid
            if (household == null)
            {
                return NotFound();
            }

            var member = await db.HouseholdMembers.FindAsync(householdId, userId);

            // only allow household owner to delete it
            if (member?.Role != HouseholdRole.Owner)
            {
                return Forbid();
            }

            db.Households.Remove(household);

            await db.SaveChangesAsync();

            // Best-effort cleanup of stock rows that referenced this household. If this fails the
            // rows are orphaned; a transactional outbox would make this delivery guaranteed.
            try
            {
                await inventoryClient.DeleteHouseholdStockAsync(householdId);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to delete inventory/shopping stock for household {HouseholdId}", householdId);
            }

            return NoContent();
        }
    }
}
