using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.Collection;

[Authorize]
[Route("api/collection")]
public sealed class CollectionController(CollectionServices service, IConfiguration configuration) : BaseApiController, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!RuntimeConfiguration.IsCatalog(configuration) || RuntimeConfiguration.IsLocalDemo(configuration))
            context.Result = new ConflictObjectResult(new { message = "This endpoint requires a migrated catalog deployment." });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    [HttpGet]
    public async Task<IActionResult> List(string? itemType = null, bool favoritesOnly = false,
        bool pinnedOnly = false, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        try { return Ok(await service.ListAsync(itemType, favoritesOnly, pinnedOnly, skip, take, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }

    [HttpGet("{itemType}/{itemId}")]
    public async Task<IActionResult> Get(string itemType, string itemId, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetAsync(itemType, itemId, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }

    [HttpPut("{itemType}/{itemId}")]
    public async Task<IActionResult> Save(string itemType, string itemId, CollectionUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await service.SaveAsync(itemType, itemId, request, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }

    [HttpDelete("{itemType}/{itemId}")]
    public async Task<IActionResult> Remove(string itemType, string itemId, CancellationToken cancellationToken)
    {
        try
        {
            await service.RemoveAsync(itemType, itemId, cancellationToken);
            return NoContent();
        }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }

    [HttpPatch("{itemType}/{itemId}")]
    public async Task<IActionResult> Patch(string itemType, string itemId, CollectionPatchRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok(await service.PatchAsync(itemType, itemId, request, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }
}
