using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prestige.Api.Endpoints.FriendshipEndpoints
{
    [ApiController]
    [Authorize]
    [Route("/api/[controller]")]
    public class FriendshipsController : ControllerBase
    {
        private readonly FriendshipService _service;

        public FriendshipsController(FriendshipService service)
        {
            _service = service;
        }

        [HttpPost("{userId}/friends/{friendId}")]
        public async Task<IActionResult> AddFriend(string userId, string friendId)
        {
            try
            {
                var res = await _service.AddFriendAsync(userId, friendId);
                return Ok(res);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("{userId}/friend-requests/{friendId}")]
        public async Task<IActionResult> SendFriendRequest(string userId, string friendId)
        {
            try
            {
                var res = await _service.SendFriendRequestAsync(userId, friendId);
                return Ok(res);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("{userId}/friend-requests/{friendId}/accept")]
        public async Task<IActionResult> AcceptFriendRequest(string userId, string friendId)
        {
            try
            {
                var res = await _service.AcceptFriendRequestAsync(userId, friendId);
                return Ok(res);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost("{userId}/friend-requests/{friendId}/decline")]
        public async Task<IActionResult> DeclineFriendRequest(string userId, string friendId)
        {
            try
            {
                var res = await _service.DeclineFriendRequestAsync(userId, friendId);
                return Ok(res);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{userId}/friend-requests")]
        public async Task<IActionResult> GetIncomingFriendRequests(string userId)
        {
            try
            {
                var requests = await _service.GetIncomingFriendRequestsAsync(userId);
                return Ok(requests);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{userId}/outgoing-friend-requests")]
        public async Task<IActionResult> GetOutgoingFriendRequests(string userId)
        {
            try
            {
                var requests = await _service.GetOutgoingFriendRequestsAsync(userId);
                return Ok(requests);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{userId}/friends")]
        public async Task<IActionResult> GetFriends(string userId)
        {
            try
            {
                var friends = await _service.GetFriendsAsync(userId);
                return Ok(friends);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{userId}/friends/{friendId}")]
        public async Task<IActionResult> GetFriend(string userId, string friendId)
        {
            try
            {
                var friend = await _service.GetFriendAsync(userId, friendId);
                if (friend == null)
                {
                    return NotFound();
                }
                return Ok(friend);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpDelete("{userId}/friends/{friendId}")]
        public async Task<IActionResult> RemoveFriend(string userId, string friendId)
        {
            try
            {
                var deleted = _service.RemoveFriend(userId, friendId);
                return Ok(deleted);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet("friend/{friendUserId}/track/{trackId}")]
        [Authorize]
        public IActionResult GetFriendUserTrackTime(string friendUserId, string trackId)
        {
            try
            {
                var totalTime = _service.GetFriendUserTrackTime(friendUserId, trackId);
                return Ok(totalTime);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("friend/{friendUserId}/artist/{artistId}")]
        [Authorize]
        public IActionResult GetFriendUserArtistTime(string friendUserId, string artistId)
        {
            try
            {
                var totalTime = _service.GetFriendUserArtistTime(friendUserId, artistId);
                return Ok(totalTime);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("friend/{friendUserId}/album/{albumId}")]
        [Authorize]
        public IActionResult GetFriendUserAlbumTime(string friendUserId, string albumId)
        {
            try
            {
                var totalTime = _service.GetFriendUserAlbumTime(friendUserId, albumId);
                return Ok(totalTime);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/listened-to-track/{trackId}")]
        public async Task<IActionResult> GetFriendsWhoListenedToTrack(string userId, string trackId)
        {
            try
            {
                var friends = await _service.GetFriendsWhoListenedToTrackAsync(userId, trackId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/listened-to-artist/{artistId}")]
        public async Task<IActionResult> GetFriendsWhoListenedToArtist(string userId, string artistId)
        {
            try
            {
                var friends = await _service.GetFriendsWhoListenedToArtistAsync(userId, artistId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/listened-to-album/{albumId}")]
        public async Task<IActionResult> GetFriendsWhoListenedToAlbum(string userId, string albumId)
        {
            try
            {
                var friends = await _service.GetFriendsWhoListenedToAlbumAsync(userId, albumId);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/compare/track/{trackId}/with/{friendId}")]
        public async Task<IActionResult> CompareTrackWithFriend(string userId, string trackId, string friendId)
        {
            try
            {
                var comparison = await _service.CompareTrackWithFriendAsync(userId, trackId, friendId);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/compare/album/{albumId}/with/{friendId}")]
        public async Task<IActionResult> CompareAlbumWithFriend(string userId, string albumId, string friendId)
        {
            try
            {
                var comparison = await _service.CompareAlbumWithFriendAsync(userId, albumId, friendId);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/compare/artist/{artistId}/with/{friendId}")]
        public async Task<IActionResult> CompareArtistWithFriend(string userId, string artistId, string friendId)
        {
            try
            {
                var comparison = await _service.CompareArtistWithFriendAsync(userId, artistId, friendId);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/tracks/{trackId}")]
        [Authorize]
        public async Task<IActionResult> GetFriendTrackDetails(string userId, string friendId, string trackId)
        {
            try
            {
                var trackDetails = await _service.GetFriendTrackDetailsAsync(userId, friendId, trackId);
                return Ok(trackDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/albums/{albumId}")]
        [Authorize]
        public async Task<IActionResult> GetFriendAlbumDetails(string userId, string friendId, string albumId)
        {
            try
            {
                var albumDetails = await _service.GetFriendAlbumDetailsAsync(userId, friendId, albumId);
                return Ok(albumDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/artists/{artistId}")]
        [Authorize]
        public async Task<IActionResult> GetFriendArtistDetails(string userId, string friendId, string artistId)
        {
            try
            {
                var artistDetails = await _service.GetFriendArtistDetailsAsync(userId, friendId, artistId);
                return Ok(artistDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/albums/{albumId}/tracks")]
        [Authorize]
        public async Task<IActionResult> GetFriendAlbumTrackRankings(string userId, string friendId, string albumId)
        {
            try
            {
                var trackRankings = await _service.GetFriendAlbumTrackRankingsAsync(userId, friendId, albumId);
                return Ok(trackRankings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/artists/{artistId}/albums")]
        [Authorize]
        public async Task<IActionResult> GetFriendArtistAlbumRankings(string userId, string friendId, string artistId)
        {
            try
            {
                var albumRankings = await _service.GetFriendArtistAlbumRankingsAsync(userId, friendId, artistId);
                return Ok(albumRankings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("{userId}/friends/{friendId}/recently-played")]
        [Authorize]
        public async Task<IActionResult> GetFriendRecentlyPlayed(string userId, string friendId)
        {
            try
            {
                var recentlyPlayed = await _service.GetFriendRecentlyPlayedAsync(userId, friendId);
                return Ok(recentlyPlayed);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while processing your request." });
            }
        }


    }
}
