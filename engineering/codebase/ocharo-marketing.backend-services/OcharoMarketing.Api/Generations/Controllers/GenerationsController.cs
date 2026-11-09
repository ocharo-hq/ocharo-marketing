using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Api.Generations.Models;
using OcharoMarketing.Application.Generations.Commands;
using OcharoMarketing.Application.Generations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Generations.Controllers;

/// <summary>Exposes text assistance and image generation jobs.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class GenerationsController(ISender sender) : ControllerBase
{
    [HttpPost("assist/text")]
    public async Task<IActionResult> Assist(AssistanceInput input, CancellationToken ct)
    {
        var command = new TextAssistCommand
        {
            Prompt = input.Prompt,
            Operation = input.Operation,
            Locale = input.Locale,
        };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("generation-jobs")]
    public async Task<IActionResult> Generate(ImageGenerationInput input, CancellationToken ct)
    {
        var command = new GenerationJobQueueCommand
        {
            Input = input,
            Requester = MarketingCaller.Requester(HttpContext)
        };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
