using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.RuntimeConfig.Settings;

namespace OcharoBlog.Api.RuntimeConfig.Controllers;

/// <summary>Exposes public runtime settings over HTTP.</summary>
[ApiController]
[Route("api/runtime-config")]
public sealed class RuntimeConfigController(OcharoAppsSettings apps) : ControllerBase
{
    /// <summary>Gets the public runtime settings.</summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<JsonObject> Get()
    {
        Response.Headers.CacheControl = "no-store";
        // The keys are the SPA's config names (OCHARO_DESK_URL), not members: a JSON object keeps them from the
        // wire preset's camelCase dictionary keys.
        var values = apps.ToRuntimeConfig().Select(item => KeyValuePair.Create(item.Key, (JsonNode?)item.Value));
        return Ok(new JsonObject(values));
    }
}
