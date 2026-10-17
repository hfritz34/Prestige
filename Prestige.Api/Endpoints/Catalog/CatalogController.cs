using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.Catalog;

[Authorize]
[Route("api/catalog")]
public sealed class CatalogController(CatalogSearchServices service, IConfiguration configuration) : BaseApiController
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(string? query, string itemType = "all", int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (!RuntimeConfiguration.IsCatalog(configuration) || RuntimeConfiguration.IsLocalDemo(configuration))
            return Conflict(new { message = "This endpoint requires a migrated catalog deployment." });
        try { return Ok(await service.SearchAsync(query, itemType, take, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }
    [HttpGet("ratings/{itemType}")]
    public async Task<IActionResult> Ratings(string itemType, [FromServices] CatalogRatingsServices ratings,
        int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        if (!RuntimeConfiguration.IsCatalog(configuration) || RuntimeConfiguration.IsLocalDemo(configuration))
            return Conflict(new { message = "This endpoint requires a migrated catalog deployment." });
        try { return Ok(await ratings.ListAsync(itemType, skip, take, cancellationToken)); }
        catch (Exceptions.Exception exception) { return HandleException(exception); }
    }
}
