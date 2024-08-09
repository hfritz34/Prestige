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
                var res = _service.AddFriend(userId, friendId);
                return Ok(res);
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


    }
}
