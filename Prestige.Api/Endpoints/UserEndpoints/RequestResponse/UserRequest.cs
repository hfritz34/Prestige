
namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse
{
    public class UserRequest
    {
        public required string Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? NickName { get; set; }

    }
}