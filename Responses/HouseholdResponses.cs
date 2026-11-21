using Identity.Entities;

namespace Identity.Responses
{
    public record MeResponse(string UserId, string Username, string? Email);
    public record MyHouseholdResponse(Guid Id, string Name, HouseholdRole Role);
    public record HouseholdResponse(Guid Id, string Name, DateTimeOffset CreatedAt, int MemberCount);
    public record HouseholdDetailResponse(Guid Id, string Name, DateTimeOffset CreatedAt, IReadOnlyList<MemberResponse> Members);
    public record MemberResponse(string UserId, string Username, HouseholdRole Role, DateTimeOffset JoinedAt);
    public record InviteResponse(Guid Id, Guid HouseholdId, string Email, InviteStatus Status, string InvitedByUserId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
    public record InvitePreviewResponse(Guid HouseholdId, string HouseholdName, string InvitedByUsername, DateTimeOffset ExpiresAt);
    public record MembershipCheckResponse(bool IsMember, HouseholdRole? Role);
}
