using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Auth;
using Prestige.Api.Data;
using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.Catalog;

// Always describes the other person, including when accepting an incoming request.
public sealed record CatalogFriendResponse(string Id, string Name, string Nickname, string ProfilePicUrl,
    string? Bio, string Status, DateTime RequestDate, DateTime? AcceptedDate);

public sealed class CatalogFriendsServices(PrestigeContext db, ClaimsPrincipal principal, IConfiguration configuration)
{
    private string UserId => CurrentAccount.GetUserId(principal, configuration)
        ?? throw new Exceptions.UnauthorizedException(401, "An authenticated account is required.");

    public async Task<List<CatalogFriendResponse>> ListAsync(string kind, int skip, int take,
        CancellationToken cancellationToken = default)
    {
        if (skip < 0 || take is < 1 or > 100)
            throw new Exceptions.ArgumentException(400, "Use skip >= 0 and take between 1 and 100.");
        var userId = UserId;
        var query = db.Friendships.AsNoTracking();
        query = kind switch
        {
            "friends" => query.Where(f => f.UserId == userId && f.Status == FriendRequestStatus.Accepted),
            "incoming" => query.Where(f => f.FriendId == userId && f.Status == FriendRequestStatus.Pending),
            "outgoing" => query.Where(f => f.UserId == userId && f.Status == FriendRequestStatus.Pending),
            _ => throw new Exceptions.ArgumentException(400, "Request direction must be incoming or outgoing.")
        };
        var rows = await query.OrderByDescending(f => f.RequestDate).ThenBy(f => f.UserId).ThenBy(f => f.FriendId)
            .Skip(skip).Take(take).Include(f => f.User).Include(f => f.Friend).ToListAsync(cancellationToken);
        return rows.Select(f => Response(kind == "incoming" ? f.User : f.Friend, f)).ToList();
    }

    public async Task<CatalogFriendResponse> ChangeAsync(string otherId, string action,
        CancellationToken cancellationToken = default)
    {
        var userId = UserId;
        if (string.IsNullOrWhiteSpace(otherId) || otherId.Length > 50 || otherId == userId)
            throw new Exceptions.ArgumentException(400, "Choose another account.");
        var other = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == otherId, cancellationToken)
            ?? throw new Exceptions.NotFoundException(404, "Account not found.");

        // Both people can act concurrently. Lock in the same order for either direction.
        var ids = new[] { userId, otherId }.Order(StringComparer.Ordinal).ToArray();
        await using var transaction = await AccountWriteTransaction.BeginAsync(db, ids[0], cancellationToken);
        if (db.Database.IsNpgsql())
            await db.Users.FromSqlInterpolated($"SELECT * FROM \"User\" WHERE \"Id\" = {ids[1]} FOR UPDATE")
                .AsNoTracking().SingleAsync(cancellationToken);
        var rows = await db.Friendships.Where(f =>
            (f.UserId == userId && f.FriendId == otherId) || (f.UserId == otherId && f.FriendId == userId))
            .ToListAsync(cancellationToken);
        var outgoing = rows.SingleOrDefault(f => f.UserId == userId);
        var incoming = rows.SingleOrDefault(f => f.UserId == otherId);
        var now = DateTime.UtcNow;
        Friendship result;
        switch (action)
        {
            case "send":
                if (outgoing?.Status == FriendRequestStatus.Accepted || outgoing?.Status == FriendRequestStatus.Pending)
                {
                    result = outgoing;
                    break;
                }
                if (incoming?.Status == FriendRequestStatus.Pending)
                    throw new Exceptions.ArgumentException(400, "This person already sent you a request. Accept or decline it first.");
                result = outgoing ?? new Friendship { UserId = userId, FriendId = otherId };
                if (outgoing == null) db.Friendships.Add(result);
                result.Status = FriendRequestStatus.Pending;
                result.RequestDate = now;
                result.AcceptedDate = null;
                break;
            case "accept":
                if (incoming?.Status == FriendRequestStatus.Accepted && outgoing?.Status == FriendRequestStatus.Accepted)
                {
                    result = incoming;
                    break;
                }
                if (incoming?.Status != FriendRequestStatus.Pending)
                    throw new Exceptions.NotFoundException(404, "Incoming request not found.");
                outgoing ??= new Friendship { UserId = userId, FriendId = otherId, RequestDate = incoming.RequestDate };
                if (!rows.Contains(outgoing)) db.Friendships.Add(outgoing);
                outgoing.Status = incoming.Status = FriendRequestStatus.Accepted;
                outgoing.AcceptedDate = incoming.AcceptedDate = now;
                result = incoming;
                break;
            case "decline":
                if (incoming == null || incoming.Status == FriendRequestStatus.Accepted)
                    throw new Exceptions.NotFoundException(404, "Incoming request not found.");
                incoming.Status = FriendRequestStatus.Declined;
                incoming.AcceptedDate = null;
                result = incoming;
                break;
            case "remove":
                // Remove both sides of an accepted relationship; do not accept or cancel pending requests.
                db.Friendships.RemoveRange(rows.Where(f => f.Status == FriendRequestStatus.Accepted));
                result = new Friendship { Status = FriendRequestStatus.Declined, RequestDate = now };
                break;
            case "cancel":
                if (outgoing?.Status == FriendRequestStatus.Pending) db.Friendships.Remove(outgoing);
                result = new Friendship { Status = FriendRequestStatus.Declined, RequestDate = now };
                break;
            default:
                throw new Exceptions.ArgumentException(400, "Unknown friendship action.");
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Response(other, result);
    }

    private static CatalogFriendResponse Response(User other, Friendship friendship) => new(other.Id,
        other.Name ?? "", other.NickName ?? "", other.ProfilePicURL ?? "", other.Bio,
        friendship.Status.ToString(), friendship.RequestDate, friendship.AcceptedDate);
}
