using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prestige.Api.Configuration;
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
                UserId = ut.User.Id,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
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
                TotalTime = ua.TotalTime,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "album")
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
                TotalTime = ua.TotalTime,
                PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "artist")
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
                        TotalTime = ut.TotalTime,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ut.TotalTime, "track")
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
                        TotalTime = ua.TotalTime,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "album")
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
                        TotalTime = ua.TotalTime,
                        PrestigeTier = PrestigeThresholds.CalculatePrestigeTier(ua.TotalTime, "artist")
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
            await ValidateFriendshipAsync(userId, friendId);

            var userTrack = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ut => ut.User.Id == userId && ut.Track.Id == trackId);

            var friendTrack = await PrestigeDb.UserTracks
                .FirstOrDefaultAsync(ut => ut.User.Id == friendId && ut.Track.Id == trackId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var userRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == trackId && r.ItemType.ToLower() == "track");
            
            var friendRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == trackId && r.ItemType.ToLower() == "track");

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
                    RatingScore = userRating != null ? (double?)userRating.PersonalScore : null,
                    Position = userRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(userTrack?.TotalTime, "Track")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendTrack?.TotalTime,
                    RatingScore = friendRating != null ? (double?)friendRating.PersonalScore : null,
                    Position = friendRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(friendTrack?.TotalTime, "Track")
                }
            };
        }

        public async Task<ItemComparisonResponse> CompareAlbumWithFriendAsync(string userId, string albumId, string friendId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var userAlbum = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Album.Id == albumId);

            var friendAlbum = await PrestigeDb.UserAlbums
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Album.Id == albumId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var userRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == albumId && r.ItemType.ToLower() == "album");
            
            var friendRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == albumId && r.ItemType.ToLower() == "album");

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
                    RatingScore = userRating != null ? (double?)userRating.PersonalScore : null,
                    Position = userRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(userAlbum?.TotalTime, "Album")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendAlbum?.TotalTime,
                    RatingScore = friendRating != null ? (double?)friendRating.PersonalScore : null,
                    Position = friendRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(friendAlbum?.TotalTime, "Album")
                }
            };
        }

        public async Task<ItemComparisonResponse> CompareArtistWithFriendAsync(string userId, string artistId, string friendId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var userArtist = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Artist.Id == artistId);

            var friendArtist = await PrestigeDb.UserArtists
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Artist.Id == artistId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var userRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == userId && r.ItemId == artistId && r.ItemType.ToLower() == "artist");
            
            var friendRating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == artistId && r.ItemType.ToLower() == "artist");

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
                    RatingScore = userRating != null ? (double?)userRating.PersonalScore : null,
                    Position = userRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(userArtist?.TotalTime, "Artist")
                },
                FriendStats = new UserStats
                {
                    ListeningTime = friendArtist?.TotalTime,
                    RatingScore = friendRating != null ? (double?)friendRating.PersonalScore : null,
                    Position = friendRating?.Position,
                    PrestigeTier = CalculatePrestigeTier(friendArtist?.TotalTime, "Artist")
                }
            };
        }

        /// <summary>
        /// Calculate prestige tier using centralized configuration system
        /// Supports both dev (low thresholds) and production (realistic thresholds) modes
        /// </summary>
        /// <param name="totalTimeSeconds">Total listening time in seconds</param>
        /// <param name="itemType">Item type: "Track", "Album", or "Artist"</param>
        /// <returns>Prestige tier name</returns>
        private string CalculatePrestigeTier(int? totalTimeSeconds, string itemType)
        {
            return PrestigeThresholds.CalculatePrestigeTier(totalTimeSeconds, itemType);
        }

        public async Task<FriendItemDetailsResponse> GetFriendTrackDetailsAsync(string userId, string friendId, string trackId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var friendTrack = await PrestigeDb.UserTracks
                .Include(ut => ut.Track)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Images)
                .Include(ut => ut.Track.Artists)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ut => ut.User.Id == friendId && ut.Track.Id == trackId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var rating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == trackId && r.ItemType.ToLower() == "track");

            return new FriendItemDetailsResponse
            {
                ItemId = trackId,
                ItemType = "Track",
                ItemName = friendTrack?.Track.Name ?? "Unknown Track",
                ItemImageUrl = friendTrack?.Track.Album.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                FriendListeningTime = friendTrack?.TotalTime,
                FriendRatingScore = rating != null ? (double?)rating.PersonalScore : null,
                FriendPosition = rating?.Position,
                FriendRankWithinAlbum = rating?.RankWithinAlbum,
                FriendPrestigeTier = CalculatePrestigeTier(friendTrack?.TotalTime, "Track"),
                IsPinned = friendTrack?.IsPinned ?? false,
                IsFavorite = friendTrack?.IsFavorite ?? false,
                AdditionalData = friendTrack?.Track
            };
        }

        public async Task<FriendItemDetailsResponse> GetFriendAlbumDetailsAsync(string userId, string friendId, string albumId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var friendAlbum = await PrestigeDb.UserAlbums
                .Include(ua => ua.Album)
                    .ThenInclude(a => a.Images)
                .Include(ua => ua.Album.Artists)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Album.Id == albumId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var rating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == albumId && r.ItemType.ToLower() == "album");

            return new FriendItemDetailsResponse
            {
                ItemId = albumId,
                ItemType = "Album",
                ItemName = friendAlbum?.Album.Name ?? "Unknown Album",
                ItemImageUrl = friendAlbum?.Album.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                FriendListeningTime = friendAlbum?.TotalTime,
                FriendRatingScore = rating != null ? (double?)rating.PersonalScore : null,
                FriendPosition = rating?.Position,
                FriendPrestigeTier = CalculatePrestigeTier(friendAlbum?.TotalTime, "Album"),
                IsPinned = friendAlbum?.IsPinned ?? false,
                IsFavorite = friendAlbum?.IsFavorite ?? false,
                AdditionalData = friendAlbum?.Album
            };
        }

        public async Task<FriendItemDetailsResponse> GetFriendArtistDetailsAsync(string userId, string friendId, string artistId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var friendArtist = await PrestigeDb.UserArtists
                .Include(ua => ua.Artist)
                    .ThenInclude(a => a.Images)
                .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Artist.Id == artistId);

            var friend = await PrestigeDb.Users.FirstOrDefaultAsync(u => u.Id == friendId);

            var rating = await PrestigeDb.Ratings
                .Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == artistId && r.ItemType.ToLower() == "artist");

            return new FriendItemDetailsResponse
            {
                ItemId = artistId,
                ItemType = "Artist",
                ItemName = friendArtist?.Artist.Name ?? "Unknown Artist",
                ItemImageUrl = friendArtist?.Artist.Images?.FirstOrDefault()?.Url ?? "",
                FriendId = friendId,
                FriendNickname = friend?.NickName ?? "Unknown",
                FriendListeningTime = friendArtist?.TotalTime,
                FriendRatingScore = rating != null ? (double?)rating.PersonalScore : null,
                FriendPosition = rating?.Position,
                FriendPrestigeTier = CalculatePrestigeTier(friendArtist?.TotalTime, "Artist"),
                IsPinned = friendArtist?.IsPinned ?? false,
                IsFavorite = friendArtist?.IsFavorite ?? false,
                AdditionalData = friendArtist?.Artist
            };
        }

        public async Task<List<FriendTrackRankingResponse>> GetFriendAlbumTrackRankingsAsync(string userId, string friendId, string albumId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var albumTracks = await PrestigeDb.Tracks
                .Include(t => t.Album)
                    .ThenInclude(a => a.Images)
                .Where(t => t.Album.Id == albumId)
                .ToListAsync();

            if (!albumTracks.Any())
            {
                return new List<FriendTrackRankingResponse>();
            }

            var trackRankings = new List<FriendTrackRankingResponse>();
            var albumImage = albumTracks.FirstOrDefault()?.Album?.Images?.FirstOrDefault()?.Url ?? "";

            foreach (var track in albumTracks)
            {
                var userTrack = await PrestigeDb.UserTracks
                    .FirstOrDefaultAsync(ut => ut.User.Id == friendId && ut.Track.Id == track.Id);

                var rating = await PrestigeDb.Ratings
                    .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == track.Id && r.ItemType.ToLower() == "track");

                trackRankings.Add(new FriendTrackRankingResponse
                {
                    TrackId = track.Id,
                    TrackName = track.Name,
                    TrackImageUrl = albumImage,
                    TrackNumber = 0,
                    Duration = track.DurationMs,
                    FriendId = friendId,
                    FriendListeningTime = userTrack?.TotalTime,
                    FriendRatingScore = rating != null ? (double?)rating.PersonalScore : null,
                    FriendPosition = rating?.Position,
                    FriendRankWithinAlbum = rating?.RankWithinAlbum,
                    FriendPrestigeTier = CalculatePrestigeTier(userTrack?.TotalTime, "Track")
                });
            }

            return trackRankings.OrderBy(t => t.FriendRankWithinAlbum ?? int.MaxValue).ToList();
        }

        public async Task<List<FriendAlbumRatingResponse>> GetFriendArtistAlbumRankingsAsync(string userId, string friendId, string artistId)
        {
            await ValidateFriendshipAsync(userId, friendId);

            var albumRankings = new List<FriendAlbumRatingResponse>();

            var artistAlbums = await PrestigeDb.Albums
                .Include(a => a.Images)
                .Include(a => a.Artists)
                .Where(a => a.Artists.Any(ar => ar.Id == artistId))
                .ToListAsync();

            foreach (var album in artistAlbums)
            {
                var userAlbum = await PrestigeDb.UserAlbums
                    .FirstOrDefaultAsync(ua => ua.User.Id == friendId && ua.Album.Id == album.Id);

                var rating = await PrestigeDb.Ratings
                    .FirstOrDefaultAsync(r => r.User.Id == friendId && r.ItemId == album.Id && r.ItemType.ToLower() == "album");

                if (userAlbum != null || rating != null)
                {
                    albumRankings.Add(new FriendAlbumRatingResponse
                    {
                        AlbumId = album.Id,
                        AlbumName = album.Name,
                        AlbumImageUrl = album.Images?.FirstOrDefault()?.Url ?? "",
                        ReleaseDate = DateTime.MinValue,
                        TrackCount = 0,
                        FriendId = friendId,
                        FriendListeningTime = userAlbum?.TotalTime,
                        FriendRatingScore = rating != null ? (double?)rating.PersonalScore : null,
                        FriendPosition = rating?.Position,
                        FriendPrestigeTier = CalculatePrestigeTier(userAlbum?.TotalTime, "Album"),
                        IsPinned = userAlbum?.IsPinned ?? false,
                        IsFavorite = userAlbum?.IsFavorite ?? false
                    });
                }
            }

            return albumRankings.OrderBy(a => a.FriendPosition ?? int.MaxValue).ToList();
        }

        private async Task ValidateFriendshipAsync(string userId, string friendId)
        {
            var friendshipExists = await PrestigeDb.Friendships
                .AnyAsync(f => (f.UserId == userId && f.FriendId == friendId && f.Status == FriendRequestStatus.Accepted) ||
                              (f.UserId == friendId && f.FriendId == userId && f.Status == FriendRequestStatus.Accepted));

            if (!friendshipExists)
            {
                throw new Exception("Users are not friends or friendship not found.");
            }
        }

    }
}

