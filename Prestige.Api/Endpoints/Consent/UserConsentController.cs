using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Endpoints.Consent.RequestResponse;
using Exception = Prestige.Api.Exceptions.Exception;

namespace Prestige.Api.Endpoints.Consent
{
    [ApiController]
    [Authorize]
    [Route("/users/{userId}/consent")]
    public class UserConsentController : BaseApiController
    {
        private readonly UserConsentServices _service;

        public UserConsentController(UserConsentServices service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserConsentAsync(string userId)
        {
            try
            {
                return Ok(await _service.GetUserConsentAsync(userId));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateUserConsentAsync(string userId, [FromBody] UpdateUserConsentRequest request)
        {
            try
            {
                return Ok(await _service.UpdateUserConsentAsync(userId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
