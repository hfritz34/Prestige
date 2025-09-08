using System.Net.Http.Headers;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.UserEndpoints.RequestResponse;
using System.Security.Claims;
using Prestige.Api.Logging;
using Microsoft.EntityFrameworkCore;
using Prestige.Api.Endpoints.Spotify.RequestResponse;

namespace Prestige.Api.Endpoints.UserEndpoints
{
    public class UserServices : BaseService
    {
        private string? UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        public UserServices(PrestigeContext db, ILogger<UserServices> logger, ClaimsPrincipal principal, IConfiguration config) : base(db, logger, principal, config)
        {
        }

        public async Task<UserResponse> CreateUserAsync(UserRequest request)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception("User authentication required for user creation");
            }

            var auth0User = await GetAuth0UserAsync();
            if(request.Id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(request.Id);
            }
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == request.Id);
            if (user != null)
            {
                return new UserResponse(user);
            }

            var newUser = new User(
                request.Id,
                request.Name ?? auth0User.Name,
                request.NickName ?? auth0User.NickName,
                request.Email ?? auth0User.Email,
                request.ProfilePicUrl ?? auth0User.Picture,
                auth0User.Identities.FirstOrDefault()?.AccessToken ?? throw Logger.TokenNotFound("Access Token"),
                auth0User.Identities.FirstOrDefault()?.RefreshToken ?? throw Logger.TokenNotFound("Refresh Token")
                );

            // Verification status will be set based on database field

            PrestigeDb.Users.Add(newUser);
            PrestigeDb.SaveChanges();
            return new UserResponse(newUser);
        }

        public async Task<(UserResponse, bool)> GetUserAsync(string id)
        {
            if (string.IsNullOrEmpty(UserAuthId))
            {
                throw new Exception("User authentication required");
            }
            
            if (id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(id);
            }

            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return (await CreateUserAsync(new UserRequest(){
                    Id = id
                }), true);
            }

            // Verification status is managed through database field

            return (new UserResponse(user), false);
        }

        public new async Task<string> GetAccessToken(string id)
        {
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            if (user.ExpiresAt < DateTime.Now.AddMinutes(-2))
            {
                // Only refresh token if it's the current user and we have authentication
                if (string.IsNullOrEmpty(UserAuthId) || id != UserAuthId.Split("|").Last())
                {
                    // For other users or background jobs, we can't refresh their token
                    throw Logger.UserUnauthorized(id);
                }
                
                var auth0User = await GetAuth0UserAsync();

                user.UpdateTokens(
                    auth0User.Identities.FirstOrDefault()?.AccessToken ?? throw Logger.ConfigurationMissing("Access Token"),
                    auth0User.Identities.FirstOrDefault()?.RefreshToken ?? throw Logger.ConfigurationMissing("Refresh Token"),
                    DateTime.Now.AddMinutes(5)
                );
            }


            PrestigeDb.SaveChanges();
            return user.AccessToken;
        }

        private async Task<Auth0UserResponse> GetAuth0UserAsync()
{
    try 
    {
        var client = new HttpClient(new HttpClientHandler())
        {
            BaseAddress = new Uri($"https://{Config["Auth0:Domain"]}"),
        };

       var tokenData = new Dictionary<string, string>
            {
                { "client_id", Config.GetSection("Auth0:ClientId").Value ??  throw Logger.ConfigurationMissing("Auth0:ClientId") },
                { "client_secret", Config.GetSection("Auth0:ClientSecret").Value ?? throw Logger.ConfigurationMissing("Auth0:ClientSecret") },
                { "audience", "https://dev-tfgyd3i2jqk0igxv.us.auth0.com/api/v2/" },
                { "grant_type", "client_credentials" }
            };

        _logger.LogInformation($"Getting management token for domain: {Config["Auth0:Domain"]}");
        var tokenRequest = new FormUrlEncodedContent(tokenData);
        var tokenResponse = await client.PostAsync("/oauth/token", tokenRequest);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError($"Token request failed: {errorContent}");
            throw new Exception($"Failed to get management token: {errorContent}");
        }

        var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        if (tokenContent == null || !tokenContent.TryGetValue("access_token", out object? value))
        {
            throw Logger.TokenNotFound("Access Token");
        }

        var accessToken = value.ToString();
        _logger.LogInformation("Successfully got management token");

        if (string.IsNullOrEmpty(UserAuthId))
        {
            throw new Exception("User authentication required for Auth0 API calls");
        }
        
        var userRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/users/{UserAuthId}");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userResponse = await client.SendAsync(userRequest);
        if (!userResponse.IsSuccessStatusCode)
        {
            var errorContent = await userResponse.Content.ReadAsStringAsync();
            _logger.LogError($"User info request failed: {errorContent}");
            throw new Exception($"Failed to get user info: {errorContent}");
        }

        var auth0User = await userResponse.Content.ReadFromJsonAsync<Auth0UserResponse>();
        return auth0User ?? throw Logger.UserNotFound(UserAuthId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in GetAuth0UserAsync");
        throw;
    }
}
        public UserResponse UpdateNickName(string id, string nickName)
        {
            if (string.IsNullOrEmpty(UserAuthId) || id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(id);
            }
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            user.UpdateNickName(nickName);
            PrestigeDb.SaveChanges();
            return new UserResponse(user);
        }

        public UserResponse UpdateProfile(string id, string? nickName, string? bio)
        {
            if (string.IsNullOrEmpty(UserAuthId) || id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(id);
            }
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            user.UpdateProfile(nickName, bio);
            PrestigeDb.SaveChanges();
            return new UserResponse(user);
        }

        public UserResponse UpdateIsSetup(string id, bool isSetup)
        {
            if (string.IsNullOrEmpty(UserAuthId) || id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(id);
            }
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            user.UpdateIsSetup(isSetup);
            PrestigeDb.SaveChanges();
            return new UserResponse(user);
        }

        public IEnumerable<UserResponse> SearchUsers(string query)
        {
            return PrestigeDb.Users
                .Where(u => EF.Functions.Contains(u.Id, query + "*") || EF.Functions.Contains(u.Name, query + "*") || EF.Functions.Contains(u.NickName, query + "*"))
                .Take(10)
                .Select(u => new UserResponse(u))
                .ToList();
        }

        public bool IsUserVerified(string userId)
        {
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == userId);
            return user?.IsVerified ?? false;
        }

        /// <summary>
        /// Updates user profile with Spotify data if profile picture has changed
        /// </summary>
        /// <param name="userId">User ID to update</param>
        /// <param name="spotifyProfile">Current Spotify profile data</param>
        /// <returns>True if profile was updated</returns>
        public bool UpdateUserFromSpotifyProfile(string userId, SpotifyUserProfileResponse spotifyProfile)
        {
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                Logger.LogWarning("User not found for profile sync: {UserId}", userId);
                return false;
            }

            if (spotifyProfile?.Images == null || !spotifyProfile.Images.Any())
            {
                Logger.LogInformation("No Spotify profile picture available for user: {UserId}", userId);
                return false;
            }

            // Get the highest quality image (usually first in array)
            var currentSpotifyProfilePic = spotifyProfile.Images.FirstOrDefault()?.Url;
            
            if (string.IsNullOrEmpty(currentSpotifyProfilePic))
            {
                Logger.LogInformation("Spotify profile picture URL is empty for user: {UserId}", userId);
                return false;
            }

            // Compare with current profile picture
            if (user.ProfilePicURL != currentSpotifyProfilePic)
            {
                Logger.LogInformation("Profile picture changed for user {UserId}. Old: {OldUrl}, New: {NewUrl}", 
                    userId, user.ProfilePicURL, currentSpotifyProfilePic);
                
                // Update user profile fields from Spotify
                user.UpdateUser(
                    name: spotifyProfile.DisplayName,
                    nickName: null, // Don't override user's custom nickname
                    email: spotifyProfile.Email,
                    profilePicURL: currentSpotifyProfilePic
                );
                
                PrestigeDb.SaveChanges();
                
                Logger.LogInformation("Successfully updated profile picture for user: {UserId}", userId);
                return true;
            }

            Logger.LogDebug("Profile picture unchanged for user: {UserId}", userId);
            return false;
        }
    }
}
