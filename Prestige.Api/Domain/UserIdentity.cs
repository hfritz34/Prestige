namespace Prestige.Api.Domain;

/// <summary>An authentication identity links to an application-owned account.</summary>
public sealed class UserIdentity
{
    public string Issuer { get; private set; } = "";
    public string Subject { get; private set; } = "";
    public string UserId { get; private set; } = "";
    public User User { get; private set; } = null!;

    private UserIdentity() { }

    public UserIdentity(string issuer, string subject, User user)
    {
        Issuer = issuer;
        Subject = subject;
        User = user;
        UserId = user.Id;
    }
}
