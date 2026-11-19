using Identity.Entities;

namespace Identity.Requests
{
    public record CreateHouseholdRequest(string Name);
    public record UpdateHouseholdRequest(string Name);
    public record UpdateMemberRequest(HouseholdRole Role);
}
