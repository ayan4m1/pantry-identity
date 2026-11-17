using Identity.Entities;

namespace Identity.Responses
{
    public record HouseholdResponse(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static HouseholdResponse From(Household household) =>
            new(household.Id, household.Name, household.CreatedAt);
    }
}
