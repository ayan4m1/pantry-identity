namespace Identity.Entities
{
    public class HouseholdMember
    {
        public Guid HouseholdId { get; set; }
        public required string UserId { get; set; } // Keycloak "sub"
        public HouseholdRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }

        public Household Household { get; set; } = null!;
    }
}
