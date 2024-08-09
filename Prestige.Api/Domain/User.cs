using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace Prestige.Api.Domain
{
        public class User
        {
                public string Id { get; private set; }
                public string Name { get; private set; }
                public string NickName { get; private set; }
                public string Email { get; private set; }
                public string ProfilePicURL { get; private set; }
                public string AccessToken { get; private set; }
                public string RefreshToken { get; private set; }
                public DateTime ExpiresAt { get; private set; } = DateTime.Now;
                public ICollection<UserTrack> UserTracks { get; private set; } = new List<UserTrack>();
                public ICollection<UserAlbum> UserAlbums { get; private set; } = new List<UserAlbum>();
                public ICollection<UserArtist> UserArtists { get; private set; } = new List<UserArtist>();
                public List<Friendship> Friendships { get; set; }
                public List<Friendship> Friends { get; set; }
                public bool IsSetup { get; private set; } = false;

                public User(string id, string name, string nickName, string email, string profilePicURL, string accessToken, string refreshToken)
                {
                        Id = id;
                        Name = name;
                        NickName = nickName;
                        Email = email;
                        ProfilePicURL = profilePicURL;
                        AccessToken = accessToken;
                        RefreshToken = refreshToken;
                }

                public void UpdateTokens(string accessToken, string refreshToken, DateTime expiresAt)
                {
                        AccessToken = accessToken;
                        RefreshToken = refreshToken;
                        ExpiresAt = expiresAt;
                }

                public void UpdateProfile(string name, string nickName, string email, string profilePicURL, string accessToken, string refreshToken)
                {
                        Name = name;
                        NickName = nickName;
                        Email = email;
                        ProfilePicURL = profilePicURL;
                        AccessToken = accessToken;
                        RefreshToken = refreshToken;
                        ExpiresAt = DateTime.Now;
                }

                public void UpdateNickName(string nickName)
                {
                        NickName = nickName;
                }

                public void UpdateIsSetup(bool isSetup)
                {
                        IsSetup = isSetup;
                }

                public void UpdateUser(string? name, string? nickName, string? email, string? profilePicURL){
                        Name = name ?? Name;
                        NickName = nickName ?? NickName;
                        Email = email ?? Email;
                        ProfilePicURL = profilePicURL ?? ProfilePicURL;
                }
        }
}
