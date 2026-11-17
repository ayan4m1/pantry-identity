using System.Text.Json.Serialization;

namespace Identity.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter<HouseholdRole>))]
    public enum HouseholdRole { Owner, Member }

    public class HouseholdMember
    {
        public Guid HouseholdId { get; set; }
        public required string UserId { get; set; } // Keycloak "sub"
        public HouseholdRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }

        public Household Household { get; set; } = null!;
    }
}
