using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Persistence;
using SkiaSharp;
using Xunit;

namespace OcharoBlog.Tests;

public sealed class MediaEditingTests
{
    private static readonly MediaEditRecipe Recipe = new(new(0, 0, 20, 30), 90, false, false, null, new("png", 90), false);

    [Fact]
    public async Task Preview_is_nonpersistent_and_save_preserves_original_with_immutable_idempotent_receipt()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var bytes=BlogHarness.Png(70);
        var source=await Upload(c,bytes);var original=source.GetProperty("sha256").GetString()!;
        var preview=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits/preview",Recipe);
        Assert.Equal(HttpStatusCode.OK,preview.StatusCode);Assert.Equal("image/png",preview.Content.Headers.ContentType!.MediaType);
        using(var image=SKBitmap.Decode(await preview.Content.ReadAsByteArrayAsync())){Assert.Equal(30,image.Width);Assert.Equal(20,image.Height);}
        Assert.Single(await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media")??[]);
        var saved=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",Recipe);saved.EnsureSuccessStatusCode();
        var result=await saved.Content.ReadFromJsonAsync<JsonElement>();var sha=result.GetProperty("sha256").GetString();Assert.NotEqual(original,sha);
        Assert.Equal(bytes,await c.GetByteArrayAsync($"/media/{original}"));
        Assert.Equal(2,(await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!.Length);
        var history=(await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!;
        Assert.Single(history);Assert.Equal(original,history[0].GetProperty("sourceSha256").GetString());
        Assert.Equal("local-operator",history[0].GetProperty("actor").GetString());
        Assert.Equal("local",history[0].GetProperty("organizationId").GetString());
        Assert.Contains("\"rotate\":90",history[0].GetProperty("recipeJson").GetString());
        (await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",Recipe)).EnsureSuccessStatusCode();
        Assert.Single((await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!);
        var db=h.Get<OcharoBlogDbContext>();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE media_edits SET Actor='untrusted'"));
    }

    [Fact]
    public async Task Optional_worker_reports_unavailable_without_pretending_removal_or_creating_copy()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var source=await Upload(c,BlogHarness.Png(80));
        var caps=await c.GetFromJsonAsync<JsonElement>("/api/blog/media/editing-capabilities");
        Assert.False(caps.GetProperty("backgroundRemoval").GetProperty("available").GetBoolean());
        var response=await c.PostAsJsonAsync($"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",Recipe with{RemoveBackground=true});
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
        Assert.Single((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!);
    }

    [Fact]
    public async Task Background_removal_precedes_crop_and_keeps_alpha_and_model_receipt()
    {
        var worker=new Worker();
        await using var h=new BlogHarness(seed:false,services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(90));
        var response=await c.PostAsJsonAsync($"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",Recipe with{RemoveBackground=true});
        response.EnsureSuccessStatusCode();var saved=await response.Content.ReadFromJsonAsync<JsonElement>();
        var sha=saved.GetProperty("sha256").GetString();
        using var image=SKBitmap.Decode(await c.GetByteArrayAsync($"/media/{sha}"));
        Assert.Equal(30,image.Width);Assert.Equal(20,image.Height);Assert.Equal(0,image.GetPixel(0,0).Alpha);
        Assert.Equal((40,60),worker.InputShape);
        var history=(await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!;
        Assert.Equal("test-model",Assert.Single(history).GetProperty("model").GetString());
    }

    [Fact]
    public async Task Revocation_before_persistence_retains_original_and_saves_no_copy()
    {
        var worker=new Worker{Expired=true};
        await using var h=new BlogHarness(seed:false,services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(90));
        var response=await c.PostAsJsonAsync($"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",Recipe);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
        Assert.Single((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!);
    }

    [Theory]
    [InlineData("{\"crop\":{\"x\":-1,\"y\":0,\"width\":20,\"height\":20},\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"rotate\":45,\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"output\":{\"format\":\"svg\",\"quality\":90}}")]
    [InlineData("{\"resize\":{\"width\":8192,\"height\":8192},\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"actor\":\"admin\",\"output\":{\"format\":\"png\",\"quality\":90}}")]
    public async Task Invalid_or_unknown_recipes_are_rejected(string json)
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var source=await Upload(c,BlogHarness.Png(70));
        var response=await c.PostAsync($"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits/preview",new StringContent(json,System.Text.Encoding.UTF8,"application/json"));
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Single((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!);
    }

    [Fact]
    public async Task Invalid_upload_never_enters_library_and_missing_source_returns404()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();using var form=new MultipartFormDataContent();
        form.Add(new ByteArrayContent(BlogHarness.Png(70)[..24]),"file","broken.png");
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/api/blog/media",form)).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/blog/media/{new string('a',64)}/edits",Recipe)).StatusCode);
    }

    [Fact]
    public async Task Receipt_failure_rolls_back_media_index_and_additive_schema_initialization_is_repeatable()
    {
        await using var h=new BlogHarness(seed:false);_ = h.Client();
        await h.Factory.Services.InitializeDatabaseAsync();await h.Factory.Services.InitializeDatabaseAsync();
        var store=h.Get<IMediaStore>();var bytes=BlogHarness.Png(110);var sha=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        await Assert.ThrowsAsync<DbUpdateException>(()=>store.StoreEditedAsync(bytes,"edited.png",new MediaEditRecord{Id=new string('1',64),SourceSha256=new string('a',64),Sha256=sha,RecipeJson="{}",OrganizationId="local",Actor="local-operator",CreatedAt=DateTimeOffset.UtcNow},default));
        var db=h.Get<OcharoBlogDbContext>();Assert.Empty(await db.MediaBlobs.ToListAsync());Assert.Empty(await db.MediaEdits.ToListAsync());
    }

    [Fact]
    public async Task Excess_concurrent_edits_are_rejected_and_capacity_recovers()
    {
        var worker=new Worker{Block=true};
        await using var h=new BlogHarness(seed:false,services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(70));var sha=source.GetProperty("sha256").GetString()!;
        var one=h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe with{RemoveBackground=true},default);
        var two=h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe with{RemoveBackground=true},default);
        try
        {
            await worker.BothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var failure=await Assert.ThrowsAsync<BlogException>(()=>h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe,default));
            Assert.Equal(429,failure.StatusCode);
        }
        finally{worker.Release.TrySetResult(true);await Task.WhenAll(one,two);}
        Assert.NotEmpty((await h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe,default)).Bytes);
    }

    [Fact]
    public async Task Different_models_have_distinct_receipts_even_when_png_bytes_match_and_jpeg_flattens_white()
    {
        var worker=new Worker{Model="first-model"};
        await using var h=new BlogHarness(seed:false,services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(70));var original=source.GetProperty("sha256").GetString();
        var recipe=Recipe with{RemoveBackground=true};
        var first=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",recipe);first.EnsureSuccessStatusCode();
        var sha=(await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString();
        worker.Model="second-model";
        (await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",recipe)).EnsureSuccessStatusCode();
        Assert.Equal(2,(await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!.Length);
        var preview=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits/preview",recipe with{Output=new("jpeg",100)});
        preview.EnsureSuccessStatusCode();Assert.Equal("image/jpeg",preview.Content.Headers.ContentType!.MediaType);
        using var bitmap=SKBitmap.Decode(await preview.Content.ReadAsByteArrayAsync());var pixel=bitmap.GetPixel(0,0);
        Assert.Equal(255,pixel.Alpha);Assert.InRange(pixel.Red,245,255);Assert.InRange(pixel.Green,245,255);Assert.InRange(pixel.Blue,245,255);
    }

    private static async Task<JsonElement> Upload(HttpClient c,byte[] bytes)
    {
        using var form=new MultipartFormDataContent();var file=new ByteArrayContent(bytes);file.Headers.ContentType=new MediaTypeHeaderValue("image/png");
        form.Add(file,"file","original.png");var response=await c.PostAsync("/api/blog/media",form);response.EnsureSuccessStatusCode();return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private sealed class Worker:IEditorialImageWorker
    {
        public bool Expired{get;init;}public string Model{get;set;}="test-model";
        public bool Block{get;init;}public TaskCompletionSource<bool> BothEntered{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entered;
        public (int,int) InputShape{get;private set;}
        public Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct)=>Task.FromResult(new ImageWorkerStatus(true,"test-model"));
        public Task EnsureEditableAsync(MediaEditActor actor,CancellationToken ct)=>Expired?Task.FromException(new BlogException("expired",401)):Task.CompletedTask;
        public async Task<(byte[] Bytes,string Model)> RemoveAsync(byte[] bytes,string contentType,CancellationToken ct)
        {
            if(Block){if(Interlocked.Increment(ref _entered)==2)BothEntered.TrySetResult(true);await Release.Task.WaitAsync(ct);}
            using var source=SKBitmap.Decode(bytes);InputShape=(source.Width,source.Height);
            using var bitmap=new SKBitmap(source.Width,source.Height,SKColorType.Rgba8888,SKAlphaType.Premul);bitmap.Erase(SKColors.Transparent);
            using var image=SKImage.FromBitmap(bitmap);using var encoded=image.Encode(SKEncodedImageFormat.Png,100);
            return (encoded.ToArray(),Model);
        }
    }
}
