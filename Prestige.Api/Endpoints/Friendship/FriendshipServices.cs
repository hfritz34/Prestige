using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.FriendshipEndpoints.RequestResponse;
using Prestige.Api.Endpoints.Prestige.RequestResponse;
using Prestige.Api.Endpoints.Profile;
using Prestige.Api.Endpoints.Spotify.RequestResponse;
using Prestige.Api.Logging;

namespace Prestige.Api.Endpoints.FriendshipEndpoints
{
    public class FriendshipService : BaseService
    {
        private string UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User not found");
        public FriendshipService(PrestigeContext db, ILogger<FriendshipService> logger, ClaimsPrincipal principal, IConfiguration config)
            : base(db, logger, principal, config)
        {

        }

        public FriendResponse AddFriend(string userId, string friendId)
        {
            var friend = PrestigeDb.Friendships
                .Include(f => f.Friend)
                .FirstOrDefault(f => f.UserId == userId && f.FriendId == friendId);
            if (friend != null)
            {
                return new FriendResponse(){
                    Id = friend.Friend.Id,
                    Name = friend.Friend.Name,
                    Nickname = friend.Friend.NickName,
                    ProfilePicUrl = friend.Friend.ProfilePicURL
                };
            }
            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
            };
            PrestigeDb.Friendships.Add(friendship);
            PrestigeDb.SaveChanges();
            PrestigeDb.Entry(friendship).Reference(f => f.Friend).Load();
            return new FriendResponse()
            {
                Id = friendship.Friend.Id,
                Name = friendship.Friend.Name,
                Nickname = friendship.Friend.NickName,
                ProfilePicUrl = friendship.Friend.ProfilePicURL
            };
        }

        public async Task<IEnumerable<UserTrackResponse>> GetFriendTopTracksAsync(string userId)
        {
            var userTracks = await PrestigeDb.UserTracks
                .Where(ut => ut.User.Id == userId)
                .OrderByDescending(ut => ut.TotalTime)
                .Take(10)
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Artists)
                        .ThenInclude(ar => ar.Images)
                .Include(ut => ut.Track.Album.Artists)
                    .ThenInclude(ar => ar.Images)
                .Include(ut => ut.User)
                .ToListAsync();

            var topTracks = userTracks.Select(ut => new UserTrackResponse()
            {
                Track = new TrackResponse(ut.Track),
                TotalTime = ut.TotalTime,
                UserId = ut.User.Id
            }).ToList();

            return topTracks;
        }

        public async Task<IEnumerable<UserAlbumResponse>> GetFriendTopAlbumsAsync(string userId)
        {
            var userAlbums = await PrestigeDb.UserAlbums
                .Where(ua => ua.User.Id == userId)
                .OrderByDescending(ua => ua.TotalTime)
                .Take(10)
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Artists)
                        .ThenInclude(ar => ar.Images)
                .Include(ua => ua.User)
                .ToListAsync();

            var topAlbums = userAlbums.Select(ua => new UserAlbumResponse()
            {
                UserId = ua.User.Id,
                Album = new AlbumResponse(ua.Album),
                TotalTime = ua.TotalTime
            }).ToList();

            return topAlbums;
        }


        public async Task<IEnumerable<UserArtistResponse>> GetFriendTopArtistsAsync(string userId)
        {
            var userArtists = await PrestigeDb.UserArtists
                .Where(ua => ua.User.Id == userId)
                .OrderByDescending(ua => ua.TotalTime)
                .Take(10)
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.User)
                .ToListAsync();

            var topArtists = userArtists.Select(ua => new UserArtistResponse()
            {
                Artist = new ArtistResponse(ua.Artist),
                UserId = ua.User.Id,
                TotalTime = ua.TotalTime
            }).ToList();

            return topArtists;
        }

        public async Task<List<FriendResponse>> GetFriendsAsync(string userId)
        {
            return await PrestigeDb.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name
                })
                .ToListAsync();
        }

        public async Task<FriendResponse?> GetFriendAsync(string userId, string friendId)
        {
            var friendshipExists = await PrestigeDb.Friendships
                .AnyAsync(f => (f.UserId == userId && f.FriendId == friendId) || (f.UserId == friendId && f.FriendId == userId));

            if (!friendshipExists)
            {
                throw new Exception("The users are not friends.");
            }

            var friend = await PrestigeDb.Friendships
                .Where(f => f.UserId == userId && f.FriendId == friendId)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name
                })
                .FirstOrDefaultAsync();

            if (friend != null)
            {
                friend.TopTracks = (await GetFriendTopTracksAsync(friendId)).ToList();
                friend.TopAlbums = (await GetFriendTopAlbumsAsync(friendId)).ToList();
                friend.TopArtists = (await GetFriendTopArtistsAsync(friendId)).ToList();

                friend.FavoriteTracks = PrestigeDb.UserTracks
                    .Include(ut => ut.Track)
                        .ThenInclude(t => t.Album)
                            .ThenInclude(a => a.Images)
                    .Include(ut => ut.Track.Album.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Include(ut => ut.Track.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Where(ut => ut.User.Id == friendId && ut.IsFavorite)
                    .Select(ut => new UserTrackResponse()
                    {
                        UserId = ut.User.Id,
                        Track = new TrackResponse(ut.Track),
                        TotalTime = ut.TotalTime
                    })
                    .ToList();

                friend.FavoriteAlbums = PrestigeDb.UserAlbums
                    .Include(ua => ua.Album)
                        .ThenInclude(a => a.Images)
                    .Include(ua => ua.Album.Artists)
                        .ThenInclude(ar => ar.Images)
                    .Where(ua => ua.User.Id == friendId && ua.IsFavorite)
                    .Select(ua => new UserAlbumResponse()
                    {
                        UserId = ua.User.Id,
                        Album = new AlbumResponse(ua.Album),
                        TotalTime = ua.TotalTime
                    })
                    .ToList();

                friend.FavoriteArtists = PrestigeDb.UserArtists
                    .Include(ua => ua.Artist)
                        .ThenInclude(a => a.Images)
                    .Where(ua => ua.User.Id == friendId && ua.IsFavorite)
                    .Select(ua => new UserArtistResponse()
                    {
                        UserId = ua.User.Id,
                        Artist = new ArtistResponse(ua.Artist),
                        TotalTime = ua.TotalTime
                    })
                    .ToList();
            }

            return friend;
        }


        public FriendResponse RemoveFriend(string userId, string friendId)
        {
            var friendship = PrestigeDb.Friendships
                .FirstOrDefault(f => f.UserId == userId && f.FriendId == friendId) ?? throw new Exception("Friendship not found");
            PrestigeDb.Friendships.Entry(friendship).Reference(f => f.Friend).Load();
            PrestigeDb.Friendships.Remove(friendship);
            PrestigeDb.SaveChanges();
            return new FriendResponse()
            {
                Id = friendship.Friend.Id,
                Name = friendship.Friend.Name,
                Nickname = friendship.Friend.NickName,
                ProfilePicUrl = friendship.Friend.ProfilePicURL
            };
        }

        public int? GetFriendUserTrackTime(string friendUserId, string trackId)
        {
            var userTrack = PrestigeDb.UserTracks
                .FirstOrDefault(userTrack => userTrack.User.Id == friendUserId && userTrack.Track.Id == trackId);

            return userTrack?.TotalTime;
        }

        public int? GetFriendUserArtistTime(string friendUserId, string artistId)
        {
            var userArtist = PrestigeDb.UserArtists
                .FirstOrDefault(userArtist => userArtist.User.Id == friendUserId && userArtist.Artist.Id == artistId);

            return userArtist?.TotalTime;
        }

        public int? GetFriendUserAlbumTime(string friendUserId, string albumId)
        {
            var userAlbum = PrestigeDb.UserAlbums
                .FirstOrDefault(userAlbum => userAlbum.User.Id == friendUserId && userAlbum.Album.Id == albumId);

            return userAlbum?.TotalTime;
        }

        public async Task<List<FriendResponse>> GetFriendsWhoListenedToTrackAsync(string userId, string trackId)
        {
            var friends = await PrestigeDb.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name
                })
                .ToListAsync();

            var friendsWhoListened = new List<FriendResponse>();
            foreach (var friend in friends)
            {
                var userTrack = await PrestigeDb.UserTracks
                    .FirstOrDefaultAsync(ut => ut.User.Id == friend.Id && ut.Track.Id == trackId);

                if (userTrack != null)
                {
                    friendsWhoListened.Add(friend);
                }
            }

            return friendsWhoListened;
        }
        public async Task<List<FriendResponse>> GetFriendsWhoListenedToArtistAsync(string userId, string artistId)
        {
            var friends = await PrestigeDb.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name
                })
                .ToListAsync();

            var friendsWhoListened = new List<FriendResponse>();
            foreach (var friend in friends)
            {
                var userArtist = await PrestigeDb.UserArtists
                    .FirstOrDefaultAsync(ua => ua.User.Id == friend.Id && ua.Artist.Id == artistId);

                if (userArtist != null)
                {
                    friendsWhoListened.Add(friend);
                }
            }

            return friendsWhoListened;
        }

        public async Task<List<FriendResponse>> GetFriendsWhoListenedToAlbumAsync(string userId, string albumId)
        {
            var friends = await PrestigeDb.Friendships
                .Where(f => f.UserId == userId)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name
                })
                .ToListAsync();

            var friendsWhoListened = new List<FriendResponse>();
            foreach (var friend in friends)
            {
                var userAlbum = await PrestigeDb.UserAlbums
                    .FirstOrDefaultAsync(ua => ua.User.Id == friend.Id && ua.Album.Id == albumId);

                if (userAlbum != null)
                {
                    friendsWhoListened.Add(friend);
                }
            }

            return friendsWhoListened;
        }

    }
}

