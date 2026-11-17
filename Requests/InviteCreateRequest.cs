using Identity.Entities;

namespace Identity.Requests
{
    public class InviteCreateRequest
    {
        public required Guid HouseholdId { get; set; }
        public required string Email { get; set; }
        public HouseholdRole Role { get; set; }
    }
}
