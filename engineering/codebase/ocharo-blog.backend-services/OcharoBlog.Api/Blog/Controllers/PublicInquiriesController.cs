using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace OcharoBlog.Api.Blog.Controllers;

[ApiController]
public sealed class PublicInquiriesController(IConfiguration configuration, IHttpClientFactory clients) : ControllerBase
{
  [HttpPost("/api/public/inquiries")]
  [RequestSizeLimit(12_000)]
  public async Task<IActionResult> Submit([FromBody] JsonElement input, CancellationToken ct)
  {
    Response.Headers.CacheControl = "no-store";
    var value = configuration["Blog:MarketingBaseUrl"];
    if (!Uri.TryCreate(
        value,
        UriKind.Absolute,
        out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0)
      return Problem(statusCode: 503, detail: "Enquiries are unavailable. Use Ocharo's listed contact channels.");
    try
    {
      using var response = await clients.CreateClient("ocharo-inquiries").PostAsJsonAsync(
          value!.TrimEnd('/') + "/api/public/inquiries",
          input,
          ct);
      return new ContentResult
      {
        StatusCode = (int)response.StatusCode,
        ContentType = "application/json",
        Content = await response.Content.ReadAsStringAsync(ct)
      };
    }
    catch (Exception e) when (!ct.IsCancellationRequested && (e is HttpRequestException or TaskCanceledException))
    {
      return Problem(statusCode: 503, detail: "We could not confirm delivery. Your enquiry may already be saved. Keep your details unchanged and retry to recover the same reference.");
    }
  }
}
