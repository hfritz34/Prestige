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

        public async Task<FriendResponse> AddFriendAsync(string userId, string friendId)
        {
            // Check if friendship already exists
            var existingFriendship = await PrestigeDb.Friendships
                .Include(f => f.Friend)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId && f.Status == FriendRequestStatus.Accepted);
            
            if (existingFriendship != null)
            {
                return new FriendResponse()
                {
                    Id = existingFriendship.Friend.Id,
                    Name = existingFriendship.Friend.Name,
                    Nickname = existingFriendship.Friend.NickName,
                    ProfilePicUrl = existingFriendship.Friend.ProfilePicURL,
                    Status = existingFriendship.Status,
                    RequestDate = existingFriendship.RequestDate,
                    AcceptedDate = existingFriendship.AcceptedDate
                };
            }

            // Create mutual friendships with accepted status (for direct add)
            var friendship1 = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = FriendRequestStatus.Accepted,
                RequestDate = DateTime.UtcNow,
                AcceptedDate = DateTime.UtcNow
            };

            var friendship2 = new Friendship
            {
                UserId = friendId,
                FriendId = userId,
                Status = FriendRequestStatus.Accepted,
                RequestDate = DateTime.UtcNow,
                AcceptedDate = DateTime.UtcNow
            };

            PrestigeDb.Friendships.AddRange(friendship1, friendship2);
            await PrestigeDb.SaveChangesAsync();
            
            await PrestigeDb.Entry(friendship1).Reference(f => f.Friend).LoadAsync();
            
            return new FriendResponse()
            {
                Id = friendship1.Friend.Id,
                Name = friendship1.Friend.Name,
                Nickname = friendship1.Friend.NickName,
                ProfilePicUrl = friendship1.Friend.ProfilePicURL,
                Status = friendship1.Status,
                RequestDate = friendship1.RequestDate,
                AcceptedDate = friendship1.AcceptedDate
            };
        }

        public async Task<FriendResponse> SendFriendRequestAsync(string userId, string friendId)
        {
            // Check if request already exists
            var existingRequest = await PrestigeDb.Friendships
                .Include(f => f.Friend)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);
            
            // Check if this is the dummy user (for auto-acceptance)
            var friendUser = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);
            var isDummyUser = friendUser?.Id == "dummy_spotify_user_12345" || 
                              friendUser?.Name?.Contains("Dummy", StringComparison.OrdinalIgnoreCase) == true || 
                              friendUser?.NickName?.Contains("Dummy", StringComparison.OrdinalIgnoreCase) == true ||
                              friendUser?.Name?.Contains("Test", StringComparison.OrdinalIgnoreCase) == true ||
                              friendUser?.NickName?.Contains("Buddy", StringComparison.OrdinalIgnoreCase) == true;
            
            // If existing request exists and it's the dummy user, update it to accepted
            if (existingRequest != null && isDummyUser && existingRequest.Status != FriendRequestStatus.Accepted)
            {
                existingRequest.Status = FriendRequestStatus.Accepted;
                existingRequest.AcceptedDate = DateTime.UtcNow;
                
                // Create reverse friendship if it doesn't exist
                var reverseExists = await PrestigeDb.Friendships
                    .AnyAsync(f => f.UserId == friendId && f.FriendId == userId);
                    
                if (!reverseExists)
                {
                    var reverseFriendship = new Friendship
                    {
                        UserId = friendId,
                        FriendId = userId,
                        Status = FriendRequestStatus.Accepted,
                        RequestDate = DateTime.UtcNow,
                        AcceptedDate = DateTime.UtcNow
                    };
                    PrestigeDb.Friendships.Add(reverseFriendship);
                }
                
                await PrestigeDb.SaveChangesAsync();
                
                return new FriendResponse()
                {
                    Id = existingRequest.Friend.Id,
                    Name = existingRequest.Friend.Name,
                    Nickname = existingRequest.Friend.NickName,
                    ProfilePicUrl = existingRequest.Friend.ProfilePicURL,
                    Status = existingRequest.Status,
                    RequestDate = existingRequest.RequestDate,
                    AcceptedDate = existingRequest.AcceptedDate
                };
            }
            
            // If existing request and not dummy user, just return it
            if (existingRequest != null)
            {
                return new FriendResponse()
                {
                    Id = existingRequest.Friend.Id,
                    Name = existingRequest.Friend.Name,
                    Nickname = existingRequest.Friend.NickName,
                    ProfilePicUrl = existingRequest.Friend.ProfilePicURL,
                    Status = existingRequest.Status,
                    RequestDate = existingRequest.RequestDate,
                    AcceptedDate = existingRequest.AcceptedDate
                };
            }

            // If we get here, no existing request found, create new one

            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = isDummyUser ? FriendRequestStatus.Accepted : FriendRequestStatus.Pending,
                RequestDate = DateTime.UtcNow,
                AcceptedDate = isDummyUser ? DateTime.UtcNow : null
            };

            PrestigeDb.Friendships.Add(friendship);

            // If dummy user, also create reverse friendship
            if (isDummyUser)
            {
                var reverseFriendship = new Friendship
                {
                    UserId = friendId,
                    FriendId = userId,
                    Status = FriendRequestStatus.Accepted,
                    RequestDate = DateTime.UtcNow,
                    AcceptedDate = DateTime.UtcNow
                };
                PrestigeDb.Friendships.Add(reverseFriendship);
            }

            await PrestigeDb.SaveChangesAsync();
            await PrestigeDb.Entry(friendship).Reference(f => f.Friend).LoadAsync();
            
            return new FriendResponse()
            {
                Id = friendship.Friend.Id,
                Name = friendship.Friend.Name,
                Nickname = friendship.Friend.NickName,
                ProfilePicUrl = friendship.Friend.ProfilePicURL,
                Status = friendship.Status,
                RequestDate = friendship.RequestDate,
                AcceptedDate = friendship.AcceptedDate
            };
        }

        public async Task<FriendResponse> AcceptFriendRequestAsync(string userId, string friendId)
        {
            var friendshipRequest = await PrestigeDb.Friendships
                .Include(f => f.Friend)
                .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == userId && f.Status == FriendRequestStatus.Pending);
            
            if (friendshipRequest == null)
            {
                throw new Exception("Friend request not found");
            }

            friendshipRequest.Status = FriendRequestStatus.Accepted;
            friendshipRequest.AcceptedDate = DateTime.UtcNow;

            // Create reverse friendship
            var reverseFriendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = FriendRequestStatus.Accepted,
                RequestDate = DateTime.UtcNow,
                AcceptedDate = DateTime.UtcNow
            };

            PrestigeDb.Friendships.Add(reverseFriendship);
            await PrestigeDb.SaveChangesAsync();

            return new FriendResponse()
            {
                Id = friendshipRequest.Friend.Id,
                Name = friendshipRequest.Friend.Name,
                Nickname = friendshipRequest.Friend.NickName,
                ProfilePicUrl = friendshipRequest.Friend.ProfilePicURL,
                Status = friendshipRequest.Status,
                RequestDate = friendshipRequest.RequestDate,
                AcceptedDate = friendshipRequest.AcceptedDate
            };
        }

        public async Task<FriendResponse> DeclineFriendRequestAsync(string userId, string friendId)
        {
            var friendshipRequest = await PrestigeDb.Friendships
                .Include(f => f.Friend)
                .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == userId && f.Status == FriendRequestStatus.Pending);
            
            if (friendshipRequest == null)
            {
                throw new Exception("Friend request not found");
            }

            friendshipRequest.Status = FriendRequestStatus.Declined;
            await PrestigeDb.SaveChangesAsync();

            return new FriendResponse()
            {
                Id = friendshipRequest.Friend.Id,
                Name = friendshipRequest.Friend.Name,
                Nickname = friendshipRequest.Friend.NickName,
                ProfilePicUrl = friendshipRequest.Friend.ProfilePicURL,
                Status = friendshipRequest.Status,
                RequestDate = friendshipRequest.RequestDate,
                AcceptedDate = friendshipRequest.AcceptedDate
            };
        }

        public async Task<List<FriendResponse>> GetFriendRequestsAsync(string userId)
        {
            return await PrestigeDb.Friendships
                .Where(f => f.FriendId == userId && f.Status == FriendRequestStatus.Pending)
                .Include(f => f.User)
                .Select(f => new FriendResponse
                {
                    Id = f.User.Id,
                    Nickname = f.User.NickName,
                    ProfilePicUrl = f.User.ProfilePicURL,
                    Name = f.User.Name,
                    Status = f.Status,
                    RequestDate = f.RequestDate,
                    AcceptedDate = f.AcceptedDate
                })
                .ToListAsync();
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
                .Where(f => f.UserId == userId && f.Status == FriendRequestStatus.Accepted)
                .Select(f => new FriendResponse
                {
                    Id = f.Friend.Id,
                    Nickname = f.Friend.NickName,
                    ProfilePicUrl = f.Friend.ProfilePicURL,
                    Name = f.Friend.Name,
                    Status = f.Status,
                    RequestDate = f.RequestDate,
                    AcceptedDate = f.AcceptedDate
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

        public async Task<ItemComparisonResponse> CompareTrackWithFriendAsync(string userId, string trackId, string friendId)
        {
            var userTrack = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ut => ut.User.Id == userId && ut.Track.Id == trackId);

            var friendTrack = await PrestigeDb.UserTracks
                .FirstOrDefaultAsync(ut => ut.User.Id == friendId && ut.Track.Id == trackId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            // Get ratings if available
            var userRating = await PrestigeDb.Set<dynamic>().FromSqlRaw(
                "SELECT PersonalScore FROM Ratings WHERE UserId = {0} AND ItemId = {1} AND ItemType = 'Track'", 
                userId, trackId).FirstOrDefaultAsync();
            
            var friendRating = await PrestigeDb.Set<dynamic>().FromSqlRaw(
                "SELECT PersonalScore FROM Ratings WHERE UserId = {0} AND ItemId = {1} AND ItemType = 'Track'", 
                friendId, trackId).FirstOrDefaultAsync();

            return new ItemComparisonResponse
            {
                ItemId = trackId,
                ItemType = "Track",
                ItemName = userTrack?.Track.Name ?? "Unknown Track",
                ItemImageUrl = userTrack?.Track.Album.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                UserStats = new UserStats
                {
                    ListeningTime = userTrack?.TotalTime,
                    RatingScore = userRating?.PersonalScore,
                    PrestigeTier = CalculatePrestigeTier(userTrack?.TotalTime, "Track")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendTrack?.TotalTime,
                    RatingScore = friendRating?.PersonalScore,
                    PrestigeTier = CalculatePrestigeTier(friendTrack?.TotalTime, "Track")
                }
            };
        }

        public async Task<ItemComparisonResponse> CompareAlbumWithFriendAsync(string userId, string albumId, string friendId)
        {
            var userAlbum = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Album.Id == albumId);

            var friendAlbum = await PrestigeDb.UserAlbums
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Album.Id == albumId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            return new ItemComparisonResponse
            {
                ItemId = albumId,
                ItemType = "Album",
                ItemName = userAlbum?.Album.Name ?? "Unknown Album",
                ItemImageUrl = userAlbum?.Album.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                UserStats = new UserStats
                {
                    ListeningTime = userAlbum?.TotalTime,
                    PrestigeTier = CalculatePrestigeTier(userAlbum?.TotalTime, "Album")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendAlbum?.TotalTime,
                    PrestigeTier = CalculatePrestigeTier(friendAlbum?.TotalTime, "Album")
                }
            };
        }

        public async Task<ItemComparisonResponse> CompareArtistWithFriendAsync(string userId, string artistId, string friendId)
        {
            var userArtist = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Artist.Id == artistId);

            var friendArtist = await PrestigeDb.UserArtists
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Artist.Id == artistId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            return new ItemComparisonResponse
            {
                ItemId = artistId,
                ItemType = "Artist",
                ItemName = userArtist?.Artist.Name ?? "Unknown Artist",
                ItemImageUrl = userArtist?.Artist.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                UserStats = new UserStats
                {
                    ListeningTime = userArtist?.TotalTime,
                    PrestigeTier = CalculatePrestigeTier(userArtist?.TotalTime, "Artist")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendArtist?.TotalTime,
                    PrestigeTier = CalculatePrestigeTier(friendArtist?.TotalTime, "Artist")
                }
            };
        }

        private string CalculatePrestigeTier(int? totalTime, string itemType)
        {
            if (!totalTime.HasValue) return "None";
            
            var timeInMinutes = totalTime.Value;
            
            return itemType switch
            {
                "Track" => timeInMinutes switch
                {
                    >= 500 => "Obsessed",
                    >= 200 => "Devoted",
                    >= 100 => "Fan",
                    >= 50 => "Casual",
                    _ => "Listener"
                },
                "Album" => timeInMinutes switch
                {
                    >= 2000 => "Obsessed",
                    >= 1000 => "Devoted", 
                    >= 500 => "Fan",
                    >= 200 => "Casual",
                    _ => "Listener"
                },
                "Artist" => timeInMinutes switch
                {
                    >= 5000 => "Obsessed",
                    >= 2500 => "Devoted",
                    >= 1000 => "Fan",
                    >= 500 => "Casual",
                    _ => "Listener"
                },
                _ => "Unknown"
            };
        }

    }
}

