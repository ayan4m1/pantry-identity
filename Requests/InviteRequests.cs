using Identity.Entities;

namespace Identity.Requests
{
    public record CreateInviteRequest(string Email, HouseholdRole Role);
}
