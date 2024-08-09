using System.Net.Http.Headers;
using Prestige.Api.Data;
using Prestige.Api.Domain;
using Prestige.Api.Endpoints.UserEndpoints.RequestResponse;
using System.Security.Claims;
using Prestige.Api.Logging;
using Microsoft.EntityFrameworkCore;

namespace Prestige.Api.Endpoints.UserEndpoints
{
    public class UserServices : BaseService
    {
        private string UserAuthId => Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User not found");
        public UserServices(PrestigeContext db, ILogger<UserServices> logger, ClaimsPrincipal principal, IConfiguration config) : base(db, logger, principal, config)
        {
        }

        public async Task<UserResponse> CreateUserAsync(UserRequest request)
        {

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

            PrestigeDb.Users.Add(newUser);
            PrestigeDb.SaveChanges();
            return new UserResponse(newUser);
        }

        public async Task<(UserResponse, bool)> GetUserAsync(string id)
        {
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
            return (new UserResponse(user), false);
        }

        public async Task<string> GetAccessToken(string id)
        {

            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            if (user.ExpiresAt < DateTime.Now.AddMinutes(-2))
            {
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
            var client = new HttpClient(new HttpClientHandler())
            {
                BaseAddress = new Uri(Config.GetSection("Auth0:Domain").Value ?? throw Logger.ConfigurationMissing("Auth0:Domain")),
            };

            var tokenData = new Dictionary<string, string>
            {
                { "client_id", Config.GetSection("Auth0:Client_Id").Value ??  throw Logger.ConfigurationMissing("Auth0:Client_Id") },
                { "client_secret", Config.GetSection("Auth0:Client_Secret").Value ?? throw Logger.ConfigurationMissing("Auth0:Client_Secret") },
                { "audience", "https://dev-u10jtlqih3lq02fh.us.auth0.com/api/v2/" },
                { "grant_type", "client_credentials" }
            };

            var tokenRequest = new FormUrlEncodedContent(tokenData);


            var tokenResponse = await client.PostAsync("/oauth/token", tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenContent = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();

            if (tokenContent == null || !tokenContent.TryGetValue("access_token", out object? value))
            {
                throw Logger.TokenNotFound("Access Token");
            }

            var accessToken = value.ToString();

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v2/users/" + UserAuthId);
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var userResponse = await client.SendAsync(userRequest);
            userResponse.EnsureSuccessStatusCode();

            var auth0User = await userResponse.Content.ReadFromJsonAsync<Auth0UserResponse>() ?? throw Logger.UserNotFound(UserAuthId);

            return auth0User;
        }

        public UserResponse UpdateNickName(string id, string nickName)
        {
            if (id != UserAuthId.Split("|").Last())
            {
                throw Logger.UserUnauthorized(id);
            }
            var user = PrestigeDb.Users.FirstOrDefault(u => u.Id == id) ?? throw Logger.UserNotFound(id);
            user.UpdateNickName(nickName);
            PrestigeDb.SaveChanges();
            return new UserResponse(user);
        }

        public UserResponse UpdateIsSetup(string id, bool isSetup)
        {
            if (id != UserAuthId.Split("|").Last())
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
    }
}
