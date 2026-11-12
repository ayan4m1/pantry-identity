using System.ComponentModel;

namespace pantry_identity.Entities
{
    public class Household
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public List<HouseholdMember> Members { get; set; } = [];
        public List<Invite> Invites { get; set; } = [];
    }
}
