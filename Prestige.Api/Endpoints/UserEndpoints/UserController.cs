using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Prestige.Api.Endpoints.UserEndpoints.RequestResponse;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.UserEndpoints
{
    [ApiController]
    [Authorize]
    [Route("/users")]
    public class UserController : BaseApiController
    {
        private readonly UserServices _service;
        public UserController(UserServices service)
        {
            _service = service;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserAsync(string id)
        {
            try
            {
                var userResponse = await _service.GetUserAsync(id);
                if (userResponse.Item2 == true)
                {
                    return Created($"/users/{userResponse.Item1.Id}", userResponse.Item1);
                }
                return Ok(userResponse.Item1);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUserAsync([FromBody] UserRequest request)
        {
            try
            {
                var userResponse = await _service.CreateUserAsync(request);
                return Created($"/users/{userResponse.Id}", userResponse);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{id}/tokens")]
        public async Task<IActionResult> GetAccessTokenAsync(string id)
        {
            try
            {
                var accessToken = await _service.GetAccessTokenAsync(id);
                return Ok(accessToken);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch("{id}/nickname")]
        public IActionResult Update(string id, NickNameRequest request)
        {
            try
            {
                var userResponse = _service.UpdateNickName(id, request.NickName);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("search")]
        public IActionResult SearchUsers([FromQuery] string query)
        {
            try
            {
                var users = _service.SearchUsers(query);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
        
        [HttpPatch("{id}/is-setup")]
        public IActionResult UpdateIsSetup(string id, [FromQuery] bool isSetup)
        {
            try
            {
                var userResponse = _service.UpdateIsSetup(id, isSetup);
                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}