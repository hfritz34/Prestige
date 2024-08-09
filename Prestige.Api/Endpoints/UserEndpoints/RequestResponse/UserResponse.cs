using Prestige.Api.Domain;

namespace Prestige.Api.Endpoints.UserEndpoints.RequestResponse
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NickName { get; set; }
        public string? Email { get; set; }
        public string ProfilePicURL { get; set; }
        public bool IsSetup { get; set; }

        public UserResponse(User user)
        {
            Id = user.Id;
            Name = user.Name;
            NickName = user.NickName;
            Email = user.Email;
            ProfilePicURL = user.ProfilePicURL;
            IsSetup = user.IsSetup;
        }   
    }
}