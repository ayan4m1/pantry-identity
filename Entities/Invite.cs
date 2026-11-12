namespace pantry_identity.Entities
{
    public class Invite
    {
        public Guid Id { get; set; }
        public Guid HouseholdId { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; } // random, URL-safe; goes in the invite link
        public required string InvitedByUserId { get; set; }
        public InviteStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public Household Household { get; set; } = null!;
    }
}
