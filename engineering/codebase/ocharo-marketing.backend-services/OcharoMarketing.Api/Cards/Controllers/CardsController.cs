using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Api.Cards.Models;
using OcharoMarketing.Application.Cards.Commands;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Cards.Controllers;

/// <summary>Exposes card previews, queued card renders and the PDF atlas of renders.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class CardsController(ISender sender) : ControllerBase
{
    [HttpPost("cards/preview")]
    public async Task<IActionResult> Preview(CardInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new CardPreviewGetQuery { Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("cards/render")]
    public async Task<IActionResult> Render(CardInput input, CancellationToken ct)
    {
        var command = new CardRenderCommand { Input = input, Requester = MarketingCaller.Requester(HttpContext) };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("atlas/export")]
    public async Task<IActionResult> Atlas(AtlasInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new CardAtlasGetQuery { AssetIds = input.AssetIds }, ct);

        return result.Match<IActionResult>(
            ok => File(ok.Data, "application/pdf", "ocharo-post-atlas.pdf"),
            fail => this.ToProblem(fail.Error));
    }
}
