using Identity.Entities;
using Identity.Responses;
using Identity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("household")]
    public class HouseholdController(
        IdentityDbContext db,
        IInventoryClient inventoryClient,
        ILogger<HouseholdController> logger
    ) : ControllerBase
    {
        [HttpPut("create")]
        [EndpointName("createHousehold")]
        public async Task<ActionResult<Guid>> Create([FromQuery] string name)
        {
            var userId = User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return NotFound();
            }

            var household = new Household() { Name = name, CreatedAt = DateTime.Now };

            household.Members.Add(new HouseholdMember() {
                UserId = userId,
                Role = HouseholdRole.Owner
            });

            var result = await db.Households.AddAsync(household);

            await db.SaveChangesAsync();

            return Ok(result.Entity.Id);
        }

        [HttpGet]
        [EndpointName("listHouseholds")]
        public async Task<ActionResult<List<HouseholdResponse>>> List()
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            return Ok(await db.Households
                .Where((household) => household.Members.Any((member) => member.UserId == userId))
                .Select((household) => new HouseholdResponse(household.Id, household.Name, household.CreatedAt))
                .ToListAsync());
        }

        [HttpGet("{id:guid}")]
        [EndpointName("getHousehold")]
        public async Task<ActionResult<HouseholdResponse>> Get(Guid id)
        {
            var household = await db.Households.FindAsync(id);

            if (household == null)
            {
                return NotFound();
            }

            return Ok(HouseholdResponse.From(household));
        }

        [HttpDelete]
        [EndpointName("deleteHousehold")]
        public async Task<IActionResult> Delete([FromQuery] Guid householdId)
        {
            var userId = User.FindFirstValue("sub");

            if (userId == null)
            {
                return Unauthorized();
            }

            var household = await db.Households.FindAsync(householdId);

            if (household == null)
            {
                return BadRequest("Household ID does not exist!");
            }

            var member = await db.HouseholdMembers.FindAsync(householdId, userId);

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
                logger.LogError(ex, "Failed to delete inventory stock for household {HouseholdId}", householdId);
            }

            return Ok();
        }
    }
}
