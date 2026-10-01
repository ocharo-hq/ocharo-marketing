using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Infrastructure.Blog;
namespace OcharoBlog.Api.Blog.Controllers;
[ApiController]
[Route("api/blog/research")]
public sealed class ResearchController(EditorialStarterImporter importer,IWebHostEnvironment environment):ControllerBase
{
    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct)=>Ok(await importer.ImportAsync(environment.ContentRootPath,ct));
}
