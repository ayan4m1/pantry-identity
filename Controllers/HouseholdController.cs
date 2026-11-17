using Identity.Entities;
using Identity.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Controllers
{
    [ApiController]
    [Route("household")]
    public class HouseholdController(IdentityDbContext db) : ControllerBase
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
        public async Task<ActionResult<List<HouseholdResponse>>> List() =>
            Ok(await db.Households.Select((household) => new HouseholdResponse(household.Id, household.Name, household.CreatedAt)).ToListAsync());

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
            var household = await db.Households.FindAsync(householdId);

            if (household == null)
            {
                return BadRequest("Household ID does not exist!");
            }

            db.Households.Remove(household);

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
