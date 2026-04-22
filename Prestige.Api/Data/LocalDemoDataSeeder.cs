using Microsoft.EntityFrameworkCore;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Data
{
    public static class LocalDemoDataSeeder
    {
        private const string DemoFriendId = "dummy_spotify_user_12345";

        public static void Seed(PrestigeContext db, IConfiguration configuration, ILogger logger)
        {
            var demoUserId = configuration["LocalDemo:UserId"] ?? "demo-user";
            var consentVersion = configuration["Consent:Version"] ?? "local-demo";

            var demoUser = EnsureUser(
                db,
                demoUserId,
                configuration["LocalDemo:Name"] ?? "Prestige Demo",
                configuration["LocalDemo:NickName"] ?? "demo",
                configuration["LocalDemo:Email"] ?? "demo@prestige.local",
                "https://placehold.co/512x512/111827/f9fafb?text=PD",
                "A local demo profile with seeded listening history.");

            var friendUser = EnsureUser(
                db,
                DemoFriendId,
                "Maya Test Buddy",
                "maya",
                "maya@prestige.local",
                "https://placehold.co/512x512/0f766e/f0fdfa?text=MT",
                "A seeded friend account for local social demos.");

            EnsureConsent(db, demoUser.Id, consentVersion);
            EnsureConsent(db, friendUser.Id, consentVersion);
            EnsureMusicCatalog(db);
            db.SaveChanges();

            EnsureListeningHistory(db, demoUser, friendUser);
            EnsureFriendship(db, demoUser.Id, friendUser.Id);
            EnsureImportHistory(db, demoUser.Id);

            db.SaveChanges();
            logger.LogInformation("Local demo data ready for user {DemoUserId}", demoUser.Id);
        }

        private static User EnsureUser(
            PrestigeContext db,
            string id,
            string name,
            string nickName,
            string email,
            string profilePicUrl,
            string bio)
        {
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.UpdateIsSetup(true);
                user.UpdateProfile(nickName, bio);
                return user;
            }

            user = new User(
                id,
                name,
                nickName,
                email,
                profilePicUrl,
                "local-demo-access-token",
                "local-demo-refresh-token");
            user.UpdateIsSetup(true);
            user.UpdateProfile(nickName, bio);
            db.Users.Add(user);
            return user;
        }

        private static void EnsureConsent(PrestigeContext db, string userId, string consentVersion)
        {
            if (!db.UserConsents.Any(consent => consent.UserId == userId))
            {
                db.UserConsents.Add(new UserConsent(userId, consentVersion));
            }
        }

        private static void EnsureMusicCatalog(PrestigeContext db)
        {
            if (db.Tracks.Any(track => track.Id == "demo_track_motion_sickness"))
            {
                return;
            }

            var seeds = new[]
            {
                new DemoTrackSeed("demo_artist_phoebe", "Phoebe Bridgers", "demo_album_stranger", "Stranger in the Alps", "demo_track_motion_sickness", "Motion Sickness", 229000, "f97316"),
                new DemoTrackSeed("demo_artist_frank", "Frank Ocean", "demo_album_blonde", "Blonde", "demo_track_nights", "Nights", 307000, "facc15"),
                new DemoTrackSeed("demo_artist_radiohead", "Radiohead", "demo_album_in_rainbows", "In Rainbows", "demo_track_weird_fishes", "Weird Fishes / Arpeggi", 318000, "2563eb"),
                new DemoTrackSeed("demo_artist_sza", "SZA", "demo_album_sos", "SOS", "demo_track_kill_bill", "Kill Bill", 153000, "16a34a"),
                new DemoTrackSeed("demo_artist_1975", "The 1975", "demo_album_like_it", "I Like It When You Sleep", "demo_track_robbers", "Robbers", 254000, "be123c")
            };

            foreach (var seed in seeds)
            {
                var artist = new Artist(
                    seed.ArtistId,
                    seed.ArtistName,
                    new[] { CreateImage(seed.ArtistName, seed.ColorHex, "artist") });

                var album = new Album(
                    seed.AlbumId,
                    seed.AlbumName,
                    new[] { CreateImage(seed.AlbumName, seed.ColorHex, "album") },
                    new[] { artist });

                var trackResponse = new TrackResponse
                {
                    Id = seed.TrackId,
                    Name = seed.TrackName,
                    DurationMs = seed.DurationMs,
                    Album = new AlbumResponse(album),
                    Artists = new[] { new ArtistResponse(artist) }
                };

                db.Tracks.Add(new Track(trackResponse, album, new[] { artist }));
            }
        }

        private static void EnsureListeningHistory(PrestigeContext db, User demoUser, User friendUser)
        {
            var tracks = db.Tracks
                .Include(track => track.Album)
                    .ThenInclude(album => album.Images)
                .Include(track => track.Album.Artists)
                .Include(track => track.Artists)
                .ThenInclude(artist => artist.Images)
                .Where(track => track.Id.StartsWith("demo_track_"))
                .ToDictionary(track => track.Id);

            AddUserTotals(db, demoUser, tracks, new Dictionary<string, int>
            {
                ["demo_track_motion_sickness"] = 5700,
                ["demo_track_nights"] = 4200,
                ["demo_track_weird_fishes"] = 3300,
                ["demo_track_kill_bill"] = 2400,
                ["demo_track_robbers"] = 1800
            }, favoriteFirstTrack: true);

            AddUserTotals(db, friendUser, tracks, new Dictionary<string, int>
            {
                ["demo_track_motion_sickness"] = 2400,
                ["demo_track_nights"] = 6300,
                ["demo_track_weird_fishes"] = 1500,
                ["demo_track_kill_bill"] = 5100,
                ["demo_track_robbers"] = 900
            }, favoriteFirstTrack: false);
        }

        private static void AddUserTotals(
            PrestigeContext db,
            User user,
            IReadOnlyDictionary<string, Track> tracks,
            IReadOnlyDictionary<string, int> secondsByTrackId,
            bool favoriteFirstTrack)
        {
            var firstTrack = secondsByTrackId.Keys.FirstOrDefault();

            foreach (var (trackId, seconds) in secondsByTrackId)
            {
                if (!tracks.TryGetValue(trackId, out var track))
                {
                    continue;
                }

                if (!db.UserTracks.Any(userTrack => userTrack.User.Id == user.Id && userTrack.Track.Id == trackId))
                {
                    var userTrack = new UserTrack(user, seconds, track);
                    if (favoriteFirstTrack && trackId == firstTrack)
                    {
                        userTrack.ToggleIsFavorite();
                        userTrack.ToggleIsPinned();
                        userTrack.UpdateRating(9.4m, 1);
                    }

                    db.UserTracks.Add(userTrack);
                }

                var album = track.Album;
                if (!db.UserAlbums.Any(userAlbum => userAlbum.User.Id == user.Id && userAlbum.Album.Id == album.Id))
                {
                    db.UserAlbums.Add(new UserAlbum(user, seconds, album));
                }

                foreach (var artist in track.Artists)
                {
                    if (!db.UserArtists.Any(userArtist => userArtist.User.Id == user.Id && userArtist.Artist.Id == artist.Id))
                    {
                        db.UserArtists.Add(new UserArtist(user, seconds * 2, artist));
                    }
                }
            }
        }

        private static void EnsureFriendship(PrestigeContext db, string userId, string friendId)
        {
            if (!db.Friendships.Any(friendship => friendship.UserId == userId && friendship.FriendId == friendId))
            {
                db.Friendships.Add(new Friendship
                {
                    UserId = userId,
                    FriendId = friendId,
                    Status = FriendRequestStatus.Accepted,
                    RequestDate = DateTime.UtcNow.AddDays(-12),
                    AcceptedDate = DateTime.UtcNow.AddDays(-12)
                });
            }

            if (!db.Friendships.Any(friendship => friendship.UserId == friendId && friendship.FriendId == userId))
            {
                db.Friendships.Add(new Friendship
                {
                    UserId = friendId,
                    FriendId = userId,
                    Status = FriendRequestStatus.Accepted,
                    RequestDate = DateTime.UtcNow.AddDays(-12),
                    AcceptedDate = DateTime.UtcNow.AddDays(-12)
                });
            }
        }

        private static void EnsureImportHistory(PrestigeContext db, string userId)
        {
            if (db.ImportHistories.Any(history => history.UserId == userId && history.FileHash == "local-demo-import"))
            {
                return;
            }

            var import = new ImportHistory(userId, "local-demo-import", "local_demo_streaming_history.json", "local-demo");
            import.SetCompleted(5284, DateTime.UtcNow.AddYears(-4), DateTime.UtcNow.AddDays(-2));
            db.ImportHistories.Add(import);
        }

        private static Image CreateImage(string label, string colorHex, string kind)
        {
            var encodedLabel = Uri.EscapeDataString($"{label} {kind}");
            return new Image($"https://placehold.co/640x640/{colorHex}/ffffff?text={encodedLabel}", 640, 640);
        }

        private sealed record DemoTrackSeed(
            string ArtistId,
            string ArtistName,
            string AlbumId,
            string AlbumName,
            string TrackId,
            string TrackName,
            int DurationMs,
            string ColorHex);
    }
}
