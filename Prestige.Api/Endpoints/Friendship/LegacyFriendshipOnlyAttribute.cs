using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Prestige.Api.Configuration;

namespace Prestige.Api.Endpoints.FriendshipEndpoints;

// Legacy routes accept caller-supplied owner IDs and direct acceptance. They must not bypass catalog consent.
public sealed class LegacyFriendshipOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        if (RuntimeConfiguration.IsCatalog(configuration) && !RuntimeConfiguration.IsLocalDemo(configuration))
            context.Result = new ConflictObjectResult(new { message = "Use the catalog friends endpoints." });
    }
}
