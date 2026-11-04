using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.Catalog;

[Authorize]
[Route("api/catalog/friends")]
public sealed class CatalogFriendsController(CatalogFriendsServices service, IConfiguration configuration) : BaseApiController
{
    [HttpGet]
    public Task<IActionResult> List(int skip = 0, int take = 50, CancellationToken cancellationToken = default) =>
        Run(async () => Ok(await service.ListAsync("friends", skip, take, cancellationToken)));

    [HttpGet("requests")]
    public Task<IActionResult> Requests(string direction = "incoming", int skip = 0, int take = 50,
        CancellationToken cancellationToken = default) =>
        Run(async () => direction is "incoming" or "outgoing"
            ? Ok(await service.ListAsync(direction, skip, take, cancellationToken))
            : BadRequest(new { message = "Request direction must be incoming or outgoing." }));

    [HttpPost("requests/{otherId}")]
    public Task<IActionResult> Send(string otherId, CancellationToken cancellationToken) =>
        Run(async () => Ok(await service.ChangeAsync(otherId, "send", cancellationToken)));

    [HttpPost("requests/{otherId}/accept")]
    public Task<IActionResult> Accept(string otherId, CancellationToken cancellationToken) =>
        Run(async () => Ok(await service.ChangeAsync(otherId, "accept", cancellationToken)));

    [HttpPost("requests/{otherId}/decline")]
    public Task<IActionResult> Decline(string otherId, CancellationToken cancellationToken) =>
        Run(async () => Ok(await service.ChangeAsync(otherId, "decline", cancellationToken)));

    [HttpDelete("requests/{otherId}")]
    public Task<IActionResult> Cancel(string otherId, CancellationToken cancellationToken) =>
        Run(async () => { await service.ChangeAsync(otherId, "cancel", cancellationToken); return NoContent(); });

    [HttpDelete("{otherId}")]
    public Task<IActionResult> Remove(string otherId, CancellationToken cancellationToken) =>
        Run(async () => { await service.ChangeAsync(otherId, "remove", cancellationToken); return NoContent(); });

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        if (!RuntimeConfiguration.IsCatalog(configuration) || RuntimeConfiguration.IsLocalDemo(configuration))
            return Conflict(new { message = "This endpoint requires a migrated catalog deployment." });
        try { return await action(); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }
}
