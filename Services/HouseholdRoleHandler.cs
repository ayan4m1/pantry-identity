using Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Identity.Services
{
    public static class Policies
    {
        public const string HouseholdMember = nameof(HouseholdMember);
        public const string HouseholdOwner = nameof(HouseholdOwner);
    }

    public record HouseholdRoleRequirement(HouseholdRole MinimumRole) : IAuthorizationRequirement;

    public class HouseholdRoleHandler(IdentityDbContext db) : AuthorizationHandler<HouseholdRoleRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, HouseholdRoleRequirement requirement)
        {
            if (context.Resource is not HttpContext http ||
                !Guid.TryParse(http.GetRouteValue("householdId")?.ToString(), out var householdId))
                return;

            var userId = context.User.FindFirstValue("sub");
            var role = await db.HouseholdMembers
                .Where(m => m.HouseholdId == householdId && m.UserId == userId)
                .Select(m => (HouseholdRole?)m.Role)
                .FirstOrDefaultAsync();

            if (role == HouseholdRole.Owner ||
                (role == HouseholdRole.Member && requirement.MinimumRole == HouseholdRole.Member))
                context.Succeed(requirement);
        }
    }
}
