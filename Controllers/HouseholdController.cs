using Identity.Entities;
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
        public async Task<IActionResult> Create([FromQuery] string name)
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

            return Ok(result.Entity.Id);
        }

        [HttpGet]
        public async Task<IActionResult> List() => Ok(await db.Households.ToListAsync());

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string id) => Ok(await db.Households.FindAsync(id));

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] string householdId)
        {
            var household = await db.Households.FindAsync(householdId);

            if (household == null)
            {
                return BadRequest("Household ID does not exist!");
            }

            db.Households.Remove(household);

            return Ok();
        }
    }
}
