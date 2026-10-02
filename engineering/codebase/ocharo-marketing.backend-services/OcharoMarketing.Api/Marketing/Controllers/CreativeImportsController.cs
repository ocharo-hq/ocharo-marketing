using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;

namespace OcharoMarketing.Api.Marketing.Controllers;

[ApiController]
[Route("api/marketing/creative-imports")]
public sealed class CreativeImportsController(
    IMarketingRepository repository,
    IAssetService assets,
    ICardRenderingService cards) : ControllerBase
{
  [HttpPost]
  [RequestSizeLimit(7_000_000)]
  public async Task<IActionResult> Import(
      IFormFile manifest,
      IFormFile image,
      [FromForm] string productId,
      CancellationToken ct)
  {
    if (manifest.Length is <= 0 or > 1_000_000 || image.Length is < 8 or > 5_242_880)
      throw new MarketingException("Choose the Studio review JSON and a PNG under 5 MiB.");
    using var manifestBuffer = new MemoryStream();
    await manifest.CopyToAsync(manifestBuffer, ct);
    using var imageBuffer = new MemoryStream();
    await image.CopyToAsync(imageBuffer, ct);
    var bytes = imageBuffer.ToArray();
    var raw = manifestBuffer.ToArray();
    JsonElement data;
    try
    {
      using var doc = JsonDocument.Parse(raw);
      data = doc.RootElement.Clone();
      Validate(data, bytes);
    }
    catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
    {
      throw new MarketingException("The Studio review manifest is invalid or does not match its image.");
    }

    var exportId = Guid.Parse(data.GetProperty("exportId").GetString()!).ToString("N");
    var key = "creative-export:" + exportId;
    var fingerprint = Hash(raw);
    var stored = await repository.GetSettingAsync(key, ct);
    if (stored is not null)
    {
      using var previous = JsonDocument.Parse(stored);
      if (previous.RootElement.GetProperty("manifestSha256").GetString() != fingerprint || previous.RootElement.GetProperty("productId").GetString() != productId)
        throw new MarketingException("This export was already imported with different content or product context.", 409);
      return Ok(new { draft = await repository.FindDraftAsync(exportId, ct), reused = true });
    }

    if (await repository.FindProductAsync(productId, ct) is null)
      throw new MarketingException("Choose an existing product as context. Studio cannot create product facts.", 404);
    var source = data.GetProperty("source");
    var derivative = data.GetProperty("derivative");
    var provenance = $"Creative visualization; Studio author submitted for review. Export {exportId}; look {source.GetProperty("lookId").GetString()}; image sha256 {Hash(bytes)}. Product facts and publication rights need independent review.";
    var asset = await assets.SaveAsync(
        bytes,
        derivative.GetProperty("fileName").GetString()!,
        provenance,
        "generated",
        ct,
        "studio-" + exportId);
    var design = data.GetProperty("design").GetProperty("document");
    var name = design.TryGetProperty("name", out var label) ? label.GetString() : "Studio look";
    var title = "Studio review · " + name;
    if (title.Length > 160)
      title = title[..160];
    var now = DateTimeOffset.UtcNow;
    var draft = new DraftModel(
        exportId,
        title,
        productId,
        cards.GetPresets().First().Id,
        "en",
        "Creative visualization — design concept for review. Confirm product details before publication.",
        ["instagram"],
        [asset.Id],
        1,
        null,
        "draft",
        now,
        now);
    await repository.SaveDraftAsync(
        draft,
        null,
        ct);
    await repository.SaveSettingAsync(
        key,
        JsonSerializer.Serialize(new { manifestSha256 = fingerprint, productId, receivingOrganization = HttpContext.Items["Ocharo.OrganizationId"], receivingUser = HttpContext.Items["Ocharo.UserId"], receivedAt = now, manifest = data }),
        ct);
    return Ok(new { draft, reused = false });
  }

  public static void Validate(JsonElement data, byte[] bytes)
  {
    if (data.GetProperty("schemaVersion").GetInt32() != 1 || !Guid.TryParse(data.GetProperty("exportId").GetString(), out _) || data.GetProperty("source").GetProperty("application").GetString() != "ocharo-studio" || data.GetProperty("source").GetProperty("revision").GetInt32() < 0 || string.IsNullOrWhiteSpace(data.GetProperty("source").GetProperty("lookId").GetString()) || string.IsNullOrWhiteSpace(data.GetProperty("derivative").GetProperty("fileName").GetString()) || data.GetProperty("sourceApproval").GetProperty("status").GetString() != "approved-for-review")
      throw new FormatException();
    var derivative = data.GetProperty("derivative");
    if (derivative.GetProperty("classification").GetString() != "creative-visualization" || derivative.GetProperty("mediaType").GetString() != "image/png" || derivative.GetProperty("bytes").GetInt64() != bytes.Length || derivative.GetProperty("sha256").GetString() != Hash(bytes) || bytes.Length < 8 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
      throw new FormatException();
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
      Canonical(writer, data.GetProperty("design").GetProperty("document"));
    if (data.GetProperty("design").GetProperty("sha256").GetString() != Hash(stream.ToArray()))
      throw new FormatException();
    foreach (var entry in data.GetProperty("assetReleases").EnumerateArray())
      CheckHash(entry.GetProperty("sha256").GetString());
    foreach (var entry in data.GetProperty("materials").EnumerateArray())
      CheckHash(entry.GetProperty("sha256").GetString());
  }

  private static void CheckHash(string? value)
  {
    if (value is null || value.Length != 64 || !value.All(c => char.IsAsciiHexDigit(c)))
      throw new FormatException();
  }

  private static string Hash(byte[] bytes)
  {
    return Convert.ToHexStringLower(SHA256.HashData(bytes));
  }

  private static void Canonical(Utf8JsonWriter writer, JsonElement value)
  {
    if (value.ValueKind == JsonValueKind.Object)
    {
      writer.WriteStartObject();
      foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
      {
        writer.WritePropertyName(property.Name);
        Canonical(writer, property.Value);
      }

      writer.WriteEndObject();
    }
    else if (value.ValueKind == JsonValueKind.Array)
    {
      writer.WriteStartArray();
      foreach (var item in value.EnumerateArray())
        Canonical(writer, item);
      writer.WriteEndArray();
    }
    else
      value.WriteTo(writer);
  }
}
