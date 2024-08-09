using Microsoft.AspNetCore.Mvc;
using Prestige.Api.ErrorResults;
using Prestige.Api.Exceptions;
using Exception = Prestige.Api.Exceptions.Exception;
using ArgumentException = Prestige.Api.Exceptions.ArgumentException;
using ArgumentNullException = Prestige.Api.Exceptions.ArgumentNullException;
using InvalidOperationException = Prestige.Api.Exceptions.InvalidOperationException;
using BadRequestResult = Prestige.Api.ErrorResults.BadRequestResult;
using NotFoundResult = Prestige.Api.ErrorResults.NotFoundResult;
using UnauthorizedResult = Prestige.Api.ErrorResults.UnauthorizedResult;

namespace Prestige.Api.Endpoints
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        [NonAction]
        public JsonResult BadRequest(Exception ex)
        {
            HttpContext.Response.StatusCode = 400;
            return new JsonResult(new BadRequestResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public JsonResult Unauthorized(Exception ex)
        {
            HttpContext.Response.StatusCode = 401;
            return new JsonResult(new UnauthorizedResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public JsonResult Forbidden(Exception ex)
        {
            HttpContext.Response.StatusCode = 403;
            return new JsonResult(new ForbiddenResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public JsonResult NotFound(Exception ex)
        {
            HttpContext.Response.StatusCode = 404;
            return new JsonResult(new NotFoundResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public JsonResult TooManyRequests(Exception ex)
        {
            HttpContext.Response.StatusCode = 429;
            return new JsonResult(new TooManyRequestsResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public JsonResult InternalServerError(Exception ex)
        {
            HttpContext.Response.StatusCode = 500;
            return new JsonResult(new InternalServerErrorResult
            {
                TraceId = HttpContext.TraceIdentifier,
                EventId = (int?)ex.Data["eventId"],
                Message = ex.Message
            });
        }

        [NonAction]
        public IActionResult HandleException(Exception ex)
        {
            if (ex is ArgumentException) return BadRequest(ex);
            else if (ex is ArgumentNullException) return BadRequest(ex);
            else if (ex is InvalidOperationException) return BadRequest(ex);
            else if (ex is NotFoundException) return NotFound(ex);
            else if (ex is UnauthorizedException) return Unauthorized(ex);
            else return InternalServerError(ex);
        }
    }
}
