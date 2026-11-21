using System.Text.Json.Serialization;

namespace Identity.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter<HouseholdRole>))]
    public enum HouseholdRole { Owner, Member }

    public class HouseholdMember
    {
        public Guid HouseholdId { get; set; }
        public required string UserId { get; set; } // Keycloak "sub"
        public required string Username { get; set; } // Keycloak "preferred_username"
        public HouseholdRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }

        public Household Household { get; set; } = null!;
    }
}
