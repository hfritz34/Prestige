namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse;

/// <summary>Public profile fields only; account email and setup state are private.</summary>
public sealed record UserSearchResponse(
    string Id, string Name, string NickName, string ProfilePicURL, string? Bio, bool IsVerified);
