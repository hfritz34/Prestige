using Microsoft.EntityFrameworkCore;
using Prestige.Api.Configuration;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints;
using Prestige.Api.Endpoints.PrestigeProgress.RequestResponse;
using Prestige.Api.Logging;
using System.Security.Claims;

namespace Prestige.Api.Endpoints.PrestigeProgress
{
    public class PrestigeProgressServices : BaseService
    {
        private string? UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public PrestigeProgressServices(PrestigeContext prestigeDb, ILogger<PrestigeProgressServices> logger, ClaimsPrincipal principal, IConfiguration config)
            : base(prestigeDb, logger, principal, config)
        {
        }

        /// <summary>
        /// Get prestige progress for the current user's item
        /// </summary>
        public async Task<PrestigeProgressResponse> GetPrestigeProgressAsync(string itemType, string itemId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw Logger.UserNotFound("Current user");
            }

            var currentUserId = UserAuthId.Split("|").Last();
            return await CalculatePrestigeProgressAsync(currentUserId, itemType, itemId);
        }

        /// <summary>
        /// Get prestige progress for a friend's item
        /// </summary>
        public async Task<PrestigeProgressResponse> GetFriendPrestigeProgressAsync(string friendId, string itemType, string itemId)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw Logger.UserNotFound("Current user");
            }

            var currentUserId = UserAuthId.Split("|").Last();
            
            // Validate friendship
            await ValidateFriendshipAsync(currentUserId, friendId);

            return await CalculatePrestigeProgressAsync(friendId, itemType, itemId);
        }

        /// <summary>
        /// Core calculation logic for prestige progress
        /// </summary>
        private async Task<PrestigeProgressResponse> CalculatePrestigeProgressAsync(string userId, string itemType, string itemId)
        {
            try
            {
                Logger.LogInformation($"Calculating prestige progress for user {userId}, itemType {itemType}, itemId {itemId}");
                
                var normalizedItemType = itemType.ToLower();
                
                // Get user's current stats for this item
                var currentStats = await GetUserItemStatsAsync(userId, itemId, normalizedItemType);
                if (currentStats == null)
                {
                    Logger.LogError($"Could not retrieve stats for user {userId}, item {itemId}");
                    throw new InvalidOperationException($"Could not retrieve stats for user {userId}, item {itemId}");
                }
                
                Logger.LogInformation($"Retrieved stats - TotalMinutes: {currentStats.TotalMinutes}");
                
                // Get item name
                var itemName = await GetItemNameAsync(itemId, normalizedItemType);
                if (string.IsNullOrEmpty(itemName))
                {
                    itemName = $"Unknown {normalizedItemType}";
                }

                // Get prestige tier thresholds (convert plural to singular for config lookup)
                var configItemType = normalizedItemType switch
                {
                    "tracks" => "track",
                    "albums" => "album", 
                    "artists" => "artist",
                    _ => normalizedItemType
                };
                
                Logger.LogInformation($"Getting thresholds for itemType: {normalizedItemType} (config key: {configItemType})");
                var thresholds = PrestigeThresholds.GetThresholds(configItemType);
                var tierNames = PrestigeThresholds.TierNames;
                
                Logger.LogInformation($"Retrieved {thresholds?.Length ?? 0} thresholds and {tierNames?.Length ?? 0} tier names");

                if (thresholds == null || thresholds.Length == 0)
                {
                    Logger.LogError($"No thresholds found for item type: {itemType}");
                    throw new ArgumentException($"No thresholds found for item type: {itemType}");
                }

                if (tierNames == null || tierNames.Length == 0)
                {
                    Logger.LogError("Tier names configuration is missing");
                    throw new InvalidOperationException("Tier names configuration is missing");
                }

                // Convert seconds to minutes for threshold comparison
                var currentMinutes = currentStats.TotalMinutes;
            
            // Find current tier
            Logger.LogInformation($"Calculating tier for {currentMinutes} minutes");
            var currentTierIndex = GetCurrentTierIndex(currentMinutes, thresholds);
            Logger.LogInformation($"Current tier index: {currentTierIndex}");
            
            var currentThresholdIndex = Math.Max(0, currentTierIndex - 1);
            var currentTier = CreateTierInfo(tierNames[currentTierIndex], thresholds, currentThresholdIndex);
            Logger.LogInformation($"Created current tier: {currentTier.DisplayName}");

            // Find next tier (if not at max level)
            PrestigeTierInfo? nextTier = null;
            var isMaxLevel = currentTierIndex >= tierNames.Length - 1;
            Logger.LogInformation($"Is max level: {isMaxLevel}");
            
            if (!isMaxLevel)
            {
                nextTier = CreateTierInfo(tierNames[currentTierIndex + 1], thresholds, currentTierIndex);
            }

            // Calculate progress percentage
            var progressPercentage = CalculateProgressPercentage(currentMinutes, currentTierIndex, thresholds);

            // Calculate time estimation
            TimeEstimation? timeEstimation = null;
            if (nextTier != null)
            {
                timeEstimation = await EstimateTimeToNextTierAsync(userId, itemId, normalizedItemType, currentStats, nextTier.Threshold);
            }

            return new PrestigeProgressResponse
            {
                ItemId = itemId,
                ItemType = CamelCaseItemType(itemType),
                ItemName = itemName,
                CurrentLevel = currentTier,
                NextLevel = nextTier, // This will be null for max level
                Progress = new ProgressStats
                {
                    CurrentValue = currentMinutes,
                    NextThreshold = isMaxLevel ? null : nextTier?.Threshold, // Explicitly null for max level
                    Percentage = isMaxLevel ? 100.0 : progressPercentage, // Always 100% at max level
                    IsMaxLevel = isMaxLevel
                },
                EstimatedTimeToNext = isMaxLevel ? null : timeEstimation // No time estimation at max level
            };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error calculating prestige progress for user {userId}, itemType {itemType}, itemId {itemId}");
                throw;
            }
        }

        /// <summary>
        /// Get user's listening statistics for a specific item
        /// </summary>
        private async Task<UserItemStats> GetUserItemStatsAsync(string userId, string itemId, string itemType)
        {
            return itemType switch
            {
                "tracks" => await GetUserTrackStatsAsync(userId, itemId),
                "albums" => await GetUserAlbumStatsAsync(userId, itemId),
                "artists" => await GetUserArtistStatsAsync(userId, itemId),
                _ => throw new ArgumentException($"Invalid item type: {itemType}")
            };
        }

        private async Task<UserItemStats> GetUserTrackStatsAsync(string userId, string trackId)
        {
            var userTrack = await PrestigeDb.UserTracks
                .FirstOrDefaultAsync(ut => ut.User.Id == userId && ut.Track.Id == trackId);

            return new UserItemStats
            {
                TotalMinutes = userTrack != null ? Math.Floor(userTrack.TotalTime / 60.0) : 0.0,
                PlayCount = userTrack != null ? 1 : 0, // Simplified - you might want to add play count tracking
                LastPlayed = userTrack?.LastUpdatedAt ?? DateTime.MinValue
            };
        }

        private async Task<UserItemStats> GetUserAlbumStatsAsync(string userId, string albumId)
        {
            var userAlbum = await PrestigeDb.UserAlbums
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Album.Id == albumId);

            return new UserItemStats
            {
                TotalMinutes = userAlbum != null ? Math.Floor(userAlbum.TotalTime / 60.0) : 0.0,
                PlayCount = userAlbum != null ? 1 : 0,
                LastPlayed = userAlbum?.LastUpdatedAt ?? DateTime.MinValue
            };
        }

        private async Task<UserItemStats> GetUserArtistStatsAsync(string userId, string artistId)
        {
            var userArtist = await PrestigeDb.UserArtists
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Artist.Id == artistId);

            return new UserItemStats
            {
                TotalMinutes = userArtist != null ? Math.Floor(userArtist.TotalTime / 60.0) : 0.0,
                PlayCount = userArtist != null ? 1 : 0,
                LastPlayed = userArtist?.LastUpdatedAt ?? DateTime.MinValue
            };
        }

        /// <summary>
        /// Get the display name for an item
        /// </summary>
        private async Task<string> GetItemNameAsync(string itemId, string itemType)
        {
            return itemType switch
            {
                "tracks" => await GetTrackNameAsync(itemId),
                "albums" => await GetAlbumNameAsync(itemId),
                "artists" => await GetArtistNameAsync(itemId),
                _ => "Unknown Item"
            };
        }

        private async Task<string> GetTrackNameAsync(string trackId)
        {
            var track = await PrestigeDb.Tracks.FirstOrDefaultAsync(t => t.Id == trackId);
            return track?.Name ?? "Unknown Track";
        }

        private async Task<string> GetAlbumNameAsync(string albumId)
        {
            var album = await PrestigeDb.Albums.FirstOrDefaultAsync(a => a.Id == albumId);
            return album?.Name ?? "Unknown Album";
        }

        private async Task<string> GetArtistNameAsync(string artistId)
        {
            var artist = await PrestigeDb.Artists.FirstOrDefaultAsync(a => a.Id == artistId);
            return artist?.Name ?? "Unknown Artist";
        }

        /// <summary>
        /// Determine current tier index based on listening minutes
        /// </summary>
        private int GetCurrentTierIndex(double minutes, int[] thresholds)
        {
            int tierIndex = 0; // Start at "None"
            
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (minutes >= thresholds[i])
                {
                    tierIndex = i + 1; // +1 because index 0 is "None"
                }
                else
                {
                    break;
                }
            }
            
            return Math.Min(tierIndex, PrestigeThresholds.TierNames.Length - 1);
        }

        /// <summary>
        /// Create tier info object with colors and metadata
        /// </summary>
        private PrestigeTierInfo CreateTierInfo(string tierName, int[] thresholds, int thresholdIndex)
        {
            var tierColors = GetTierColors();
            var tierImageNames = GetTierImageNames();
            
            return new PrestigeTierInfo
            {
                Tier = tierName.ToLower(),
                DisplayName = tierName,
                Color = tierColors.GetValueOrDefault(tierName.ToLower(), "#888888"),
                ImageName = tierImageNames.GetValueOrDefault(tierName.ToLower(), $"{tierName.ToLower()}_prestige"),
                Threshold = thresholdIndex >= 0 && thresholdIndex < thresholds.Length ? thresholds[thresholdIndex] : 
                           (tierName.ToLower() == "prestige" && thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 0)
            };
        }

        /// <summary>
        /// Calculate progress percentage between current and next tier
        /// </summary>
        private double CalculateProgressPercentage(double currentMinutes, int currentTierIndex, int[] thresholds)
        {
            // If at max level, return 100%
            if (currentTierIndex >= PrestigeThresholds.TierNames.Length - 1)
            {
                return 100.0;
            }

            // If at "None" tier
            if (currentTierIndex == 0)
            {
                if (thresholds.Length > 0)
                {
                    return Math.Min((currentMinutes / thresholds[0]) * 100.0, 100.0);
                }
                return 0.0;
            }

            // Between two tiers
            var currentThreshold = thresholds[currentTierIndex - 1];
            
            if (currentTierIndex < thresholds.Length)
            {
                var nextThreshold = thresholds[currentTierIndex];
                var progressInTier = currentMinutes - currentThreshold;
                var tierRange = nextThreshold - currentThreshold;
                
                return tierRange > 0 ? Math.Min((progressInTier / tierRange) * 100.0, 100.0) : 0.0;
            }

            return 100.0;
        }

        /// <summary>
        /// Estimate time to reach next tier based on listening patterns
        /// </summary>
        private async Task<TimeEstimation> EstimateTimeToNextTierAsync(string userId, string itemId, string itemType, UserItemStats currentStats, double nextThreshold)
        {
            var minutesNeeded = nextThreshold - currentStats.TotalMinutes;
            
            if (minutesNeeded <= 0)
            {
                return new TimeEstimation
                {
                    MinutesRemaining = 0,
                    FormattedTime = "0m",
                    EstimationType = "already_achieved"
                };
            }

            // Get recent listening activity for this specific item (last 30 days)
            var recentActivity = await GetRecentItemActivityAsync(userId, itemId, itemType, 30);
            
            if (recentActivity.TotalMinutesLast30Days > 0)
            {
                // Calculate based on recent activity
                var dailyAverage = recentActivity.TotalMinutesLast30Days / 30.0;
                
                // Apply intelligent scaling based on how close they are to the next tier
                // If very close (< 60 minutes), assume higher engagement
                if (minutesNeeded < 60)
                {
                    dailyAverage = Math.Max(dailyAverage, 10.0); // At least 10 min/day when close
                }
                else if (minutesNeeded < 180)
                {
                    dailyAverage = Math.Max(dailyAverage, 5.0); // At least 5 min/day when moderately close
                }
                
                var estimatedDays = minutesNeeded / dailyAverage;
                
                // Cap estimates at reasonable maximums
                if (estimatedDays > 365)
                {
                    estimatedDays = 365; // Cap at 1 year
                }
                
                return new TimeEstimation
                {
                    MinutesRemaining = minutesNeeded,
                    FormattedTime = FormatTimeEstimate(estimatedDays),
                    EstimationType = "based_on_recent_activity"
                };
            }
            else
            {
                // Fallback: Use intelligent defaults based on item type
                double dailyEstimate = itemType switch
                {
                    "tracks" => 3.5,  // One play per day for a track
                    "albums" => 15.0,  // Partial album listen per day
                    "artists" => 10.0, // Few tracks per day
                    _ => 5.0
                };
                
                // Scale up if they're very close to the next tier
                if (minutesNeeded < 60)
                {
                    dailyEstimate *= 2.0; // Double the rate when very close
                }
                
                var estimatedDays = minutesNeeded / dailyEstimate;
                
                // Cap at reasonable maximum
                if (estimatedDays > 365)
                {
                    estimatedDays = 365;
                }
                
                return new TimeEstimation
                {
                    MinutesRemaining = minutesNeeded,
                    FormattedTime = FormatTimeEstimate(estimatedDays),
                    EstimationType = "minimum_rate_estimate"
                };
            }
        }

        /// <summary>
        /// Get recent listening activity for time estimation
        /// </summary>
        private async Task<RecentItemActivity> GetRecentItemActivityAsync(string userId, string itemId, string itemType, int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            
            // This is a simplified version. In a real implementation, you might have
            // a more detailed listening history table to track daily/weekly patterns
            return itemType switch
            {
                "tracks" => await GetRecentTrackActivityAsync(userId, itemId, cutoffDate),
                "albums" => await GetRecentAlbumActivityAsync(userId, itemId, cutoffDate),
                "artists" => await GetRecentArtistActivityAsync(userId, itemId, cutoffDate),
                _ => new RecentItemActivity()
            };
        }

        private async Task<RecentItemActivity> GetRecentTrackActivityAsync(string userId, string trackId, DateTime cutoffDate)
        {
            var userTrack = await PrestigeDb.UserTracks
                .FirstOrDefaultAsync(ut => ut.User.Id == userId && ut.Track.Id == trackId);

            // Better estimation: if played recently, estimate based on track length
            if (userTrack?.LastUpdatedAt >= cutoffDate)
            {
                // Tracks are typically 3-4 minutes, estimate 10-20 plays in last 30 days if active
                var estimatedPlaysLast30Days = 15.0; // Reasonable estimate for active listening
                var averageTrackLength = 3.5; // minutes
                
                return new RecentItemActivity
                {
                    TotalMinutesLast30Days = estimatedPlaysLast30Days * averageTrackLength
                };
            }

            return new RecentItemActivity();
        }

        private async Task<RecentItemActivity> GetRecentAlbumActivityAsync(string userId, string albumId, DateTime cutoffDate)
        {
            var userAlbum = await PrestigeDb.UserAlbums
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Album.Id == albumId);

            if (userAlbum?.LastUpdatedAt >= cutoffDate)
            {
                // Albums are typically 40-60 minutes, estimate 3-5 plays in last 30 days if active
                var estimatedPlaysLast30Days = 4.0;
                var averageAlbumLength = 45.0; // minutes
                
                return new RecentItemActivity
                {
                    TotalMinutesLast30Days = estimatedPlaysLast30Days * averageAlbumLength
                };
            }

            return new RecentItemActivity();
        }

        private async Task<RecentItemActivity> GetRecentArtistActivityAsync(string userId, string artistId, DateTime cutoffDate)
        {
            var userArtist = await PrestigeDb.UserArtists
                .FirstOrDefaultAsync(ua => ua.User.Id == userId && ua.Artist.Id == artistId);

            if (userArtist?.LastUpdatedAt >= cutoffDate)
            {
                // For artists, estimate based on typical listening patterns
                // Active listener might play 20-30 tracks from an artist in 30 days
                var estimatedTracksLast30Days = 25.0;
                var averageTrackLength = 3.5; // minutes
                
                return new RecentItemActivity
                {
                    TotalMinutesLast30Days = estimatedTracksLast30Days * averageTrackLength
                };
            }

            return new RecentItemActivity();
        }

        /// <summary>
        /// Format time estimate into human-readable string (hours as maximum unit)
        /// </summary>
        private string FormatTimeEstimate(double days)
        {
            // Convert days to total hours
            var totalHours = days * 24;
            
            // If less than 1 hour, show minutes
            if (totalHours < 1)
            {
                var minutes = totalHours * 60;
                return $"{Math.Ceiling(minutes)}m";
            }
            
            // For any amount of time, show in hours (capped at 999h for display)
            if (totalHours > 999)
            {
                return "999+h";
            }
            
            return $"{Math.Ceiling(totalHours)}h";
        }

        /// <summary>
        /// Validate friendship between users
        /// </summary>
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

        /// <summary>
        /// Convert item type to proper case for response
        /// </summary>
        private string CamelCaseItemType(string itemType)
        {
            return itemType.ToLower() switch
            {
                "tracks" => "tracks",
                "albums" => "albums", 
                "artists" => "artists",
                _ => itemType.ToLower()
            };
        }

        /// <summary>
        /// Get tier color mapping
        /// </summary>
        private Dictionary<string, string> GetTierColors()
        {
            return new Dictionary<string, string>
            {
                { "none", "#808080" },
                { "bronze", "#CD7F32" },
                { "silver", "#C0C0C0" },
                { "gold", "#FFD700" },
                { "emerald", "#50C878" },
                { "amber", "#FFBF00" },
                { "amethyst", "#9966CC" },
                { "quartz", "#E8E8E8" },
                { "diamond", "#B9F2FF" },
                { "jade", "#00A86B" },
                { "ruby", "#E0115F" },
                { "pearl", "#F0EAD6" },
                { "loveydovey", "#FF69B4" },
                { "tourmaline", "#86608E" },
                { "topaz", "#FFC87C" },
                { "tanazanite", "#243B82" },
                { "prestige", "#FF4500" }
            };
        }

        /// <summary>
        /// Get tier image name mapping
        /// </summary>
        private Dictionary<string, string> GetTierImageNames()
        {
            return new Dictionary<string, string>
            {
                { "none", "none_prestige" },
                { "bronze", "bronze_prestige" },
                { "silver", "silver_prestige" },
                { "gold", "gold_prestige" },
                { "emerald", "emerald_prestige" },
                { "amber", "amber_prestige" },
                { "amethyst", "amethyst_prestige" },
                { "quartz", "quartz_prestige" },
                { "diamond", "diamond_prestige" },
                { "jade", "jade_prestige" },
                { "ruby", "ruby_prestige" },
                { "pearl", "pearl_prestige" },
                { "loveydovey", "loveydovey_prestige" },
                { "tourmaline", "tourmaline_prestige" },
                { "topaz", "topaz_prestige" },
                { "tanazanite", "tanazanite_prestige" },
                { "prestige", "prestige_prestige" }
            };
        }
    }

    /// <summary>
    /// Helper classes for internal calculations
    /// </summary>
    internal class UserItemStats
    {
        public double TotalMinutes { get; set; }
        public int PlayCount { get; set; }
        public DateTime LastPlayed { get; set; }
    }

    internal class RecentItemActivity
    {
        public double TotalMinutesLast30Days { get; set; }
    }
}