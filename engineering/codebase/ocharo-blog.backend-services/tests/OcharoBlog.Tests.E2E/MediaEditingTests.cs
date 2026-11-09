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

namespace OcharoBlog.Tests.E2E;

[Collection(MediaEditingCapacity.Name)]
public sealed class MediaEditingTests
{
    private static readonly MediaEditRecipe Recipe =
        new(new(0, 0, 20, 30), 90, false, false, null, new("png", 90), false);

    [Fact]
    public async Task EditSave_ShouldKeepTheOriginalWithAnIdempotentReceipt_WhenPreviewedFirst()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var bytes=BlogHarness.Png(70);
        var source=await Upload(c,bytes);var original=source.GetProperty("sha256").GetString()!;
        var preview=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits/preview",Recipe);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        preview.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        using(var image=SKBitmap.Decode(await preview.Content.ReadAsByteArrayAsync()))
        {
            image.Width.Should().Be(30);image.Height.Should().Be(20);
        }
        (await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media")??[]).Should().ContainSingle();
        var saved=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",Recipe);saved.EnsureSuccessStatusCode();
        var result=await saved.Content.ReadFromJsonAsync<JsonElement>();
        var sha=result.GetProperty("sha256").GetString();
        sha.Should().NotBe(original);
        (await c.GetByteArrayAsync($"/media/{original}")).Should().Equal(bytes);
        ((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!.Length).Should().Be(2);
        var history=(await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!;
        history.Should().ContainSingle();history[0].GetProperty("sourceSha256").GetString().Should().Be(original);
        history[0].GetProperty("actor").GetString().Should().Be(EditorialSignIn.Owner.Subject);
        history[0].GetProperty("organizationId").GetString().Should().Be(EditorialSignIn.Organization.ToString("D"));
        // PostgreSQL keeps the recipe as jsonb, which hands back its own spacing and key order: compare values.
        var recipe=JsonDocument.Parse(history[0].GetProperty("recipeJson").GetString()!).RootElement;
        recipe.GetProperty("rotate").GetInt32().Should().Be(90);
        (await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",Recipe)).EnsureSuccessStatusCode();
        ((await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!).Should().ContainSingle();
        var db=h.Get<OcharoBlogDbContext>();
        await FluentActions.Awaiting(
            ()=>db.Database.ExecuteSqlRawAsync("UPDATE blog.media_edits SET actor='untrusted'"))
            .Should()
            .ThrowExactlyAsync<Npgsql.PostgresException>();
    }

    [Fact]
    public async Task BackgroundRemoval_ShouldReturn503WithoutACopy_WhenNoWorkerIsConfigured()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var source=await Upload(c,BlogHarness.Png(80));
        var caps=await c.GetFromJsonAsync<JsonElement>("/api/blog/media/editing-capabilities");
        caps.GetProperty("backgroundRemoval").GetProperty("available").GetBoolean().Should().BeFalse();
        var response=
            await c.PostAsJsonAsync(
                $"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",
                Recipe with{RemoveBackground=true});
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        ((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!).Should().ContainSingle();
    }

    [Fact]
    public async Task BackgroundRemoval_ShouldRunBeforeCropAndKeepAlpha_WhenTheWorkerAnswers()
    {
        var worker=new Worker();
        await using var h=
            new BlogHarness(
                seed:false,
                services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(90));
        var response=
            await c.PostAsJsonAsync(
                $"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",
                Recipe with{RemoveBackground=true});
        response.EnsureSuccessStatusCode();var saved=await response.Content.ReadFromJsonAsync<JsonElement>();
        var sha=saved.GetProperty("sha256").GetString();
        using var image=SKBitmap.Decode(await c.GetByteArrayAsync($"/media/{sha}"));
        image.Width.Should().Be(30);image.Height.Should().Be(20);image.GetPixel(0,0).Alpha.Should().Be(0);
        worker.InputShape.Should().Be((40,60));
        var history=(await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!;
        history.Should().ContainSingle().Subject.GetProperty("model").GetString().Should().Be("test-model");
    }

    [Fact]
    public async Task EditSave_ShouldSaveNoCopy_WhenAccessIsRevokedBeforePersistence()
    {
        var worker=new Worker{Expired=true};
        await using var h=
            new BlogHarness(
                seed:false,
                services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();var source=await Upload(c,BlogHarness.Png(90));
        var response=
            await c.PostAsJsonAsync($"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits",Recipe);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!).Should().ContainSingle();
    }

    [Theory]
    [InlineData(
        "{\"crop\":{\"x\":-1,\"y\":0,\"width\":20,\"height\":20},\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"rotate\":45,\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"output\":{\"format\":\"svg\",\"quality\":90}}")]
    [InlineData("{\"resize\":{\"width\":8192,\"height\":8192},\"output\":{\"format\":\"png\",\"quality\":90}}")]
    [InlineData("{\"actor\":\"admin\",\"output\":{\"format\":\"png\",\"quality\":90}}")]
    public async Task EditPreview_ShouldReturn400_WhenTheRecipeIsInvalidOrUnknown(string json)
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();var source=await Upload(c,BlogHarness.Png(70));
        var response=
            await c.PostAsync(
                $"/api/blog/media/{source.GetProperty("sha256").GetString()}/edits/preview",
                new StringContent(json,System.Text.Encoding.UTF8,"application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!).Should().ContainSingle();
    }

    [Fact]
    public async Task Upload_ShouldStayOutOfTheLibraryAndEditsReturn404_WhenInvalid()
    {
        await using var h=new BlogHarness(seed:false);var c=h.Client();using var form=new MultipartFormDataContent();
        form.Add(new ByteArrayContent(BlogHarness.Png(70)[..24]),"file","broken.png");
        ((await c.PostAsync("/api/blog/media",form)).StatusCode).Should().Be(HttpStatusCode.BadRequest);
        ((await c.GetFromJsonAsync<JsonElement[]>("/api/blog/media"))!).Should().BeEmpty();
        ((await c.PostAsJsonAsync($"/api/blog/media/{new string('a',64)}/edits",Recipe)).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EditedStore_ShouldRollBackTheMediaIndex_WhenTheReceiptFails()
    {
        await using var h=new BlogHarness(seed:false);_ = h.Client();
        // The harness's database is current already: each further migrate run finds it so and changes nothing.
        foreach (var _ in new[] { 1, 2 })
        {
            var migrate=await BlogDatabase.MigrateAsync(h.Database);
            (migrate.Exit==0).Should().BeTrue(migrate.Error);
        }
        var store=h.Get<IMediaStore>();
        var bytes=BlogHarness.Png(110);
        var sha=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        await FluentActions.Awaiting(()=>
            store.StoreEditedAsync(
                bytes,
                "edited.png",
                new MediaEditRecord
                {
                    Id=new string('1',64),
                    SourceSha256=new string('a',64),
                    Sha256=sha,
                    RecipeJson="{}",
                    OrganizationId=EditorialSignIn.Organization.ToString("D"),
                    Actor=EditorialSignIn.Owner.Subject,
                    CreatedAt=DateTimeOffset.UtcNow
                },
                default))
            .Should()
            .ThrowExactlyAsync<DbUpdateException>();
        var db=h.Get<OcharoBlogDbContext>();
        (await db.MediaBlobs.ToListAsync()).Should().BeEmpty();
        (await db.MediaEdits.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task EditPreview_ShouldRefuseAsBusyThenRecover_WhenEditsExceedCapacity()
    {
        var worker=new Worker{Block=true};
        await using var h=
            new BlogHarness(
                seed:false,
                services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();
        var source=await Upload(c,BlogHarness.Png(70));
        var sha=source.GetProperty("sha256").GetString()!;
        var one=h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe with{RemoveBackground=true},default);
        var two=h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe with{RemoveBackground=true},default);
        try
        {
            await worker.BothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var failure=Refusals.Of(await h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe,default));
            failure.Type.Should().Be(AppErrorType.TooManyRequests);
        }
        finally{worker.Release.TrySetResult(true);await Task.WhenAll(one,two);}
        ((await h.Get<IMediaEditingService>().PreviewAsync(sha,Recipe,default)).ValueOrThrow().Bytes)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task EditSave_ShouldGiveEachModelItsReceipt_WhenTheBytesMatch()
    {
        var worker=new Worker{Model="first-model"};
        await using var h=
            new BlogHarness(
                seed:false,
                services:s=>s.Replace(ServiceDescriptor.Singleton<IEditorialImageWorker>(worker)));
        var c=h.Client();
        var source=await Upload(c,BlogHarness.Png(70));
        var original=source.GetProperty("sha256").GetString();
        var recipe=Recipe with{RemoveBackground=true};
        var first=await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",recipe);first.EnsureSuccessStatusCode();
        var sha=(await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString();
        worker.Model="second-model";
        (await c.PostAsJsonAsync($"/api/blog/media/{original}/edits",recipe)).EnsureSuccessStatusCode();
        ((await c.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{sha}/edits"))!.Length).Should().Be(2);
        var preview=
            await c.PostAsJsonAsync($"/api/blog/media/{original}/edits/preview",recipe with{Output=new("jpeg",100)});
        preview.EnsureSuccessStatusCode();preview.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        using var bitmap=SKBitmap.Decode(await preview.Content.ReadAsByteArrayAsync());var pixel=bitmap.GetPixel(0,0);
        pixel.Alpha.Should().Be(255);
        pixel.Red.Should().BeInRange(245, 255);
        pixel.Green.Should().BeInRange(245, 255);
        pixel.Blue.Should().BeInRange(245, 255);
    }

    private static async Task<JsonElement> Upload(HttpClient c,byte[] bytes)
    {
        using var form=new MultipartFormDataContent();
        var file=new ByteArrayContent(bytes);
        file.Headers.ContentType=new MediaTypeHeaderValue("image/png");
        form.Add(file,"file","original.png");
        var response=await c.PostAsync("/api/blog/media",form);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private sealed class Worker:IEditorialImageWorker
    {
        public bool Expired{get;init;}public string Model{get;set;}="test-model";
        public bool Block
        {
            get;init;
        }public TaskCompletionSource<bool> BothEntered{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entered;
        public (int,int) InputShape{get;private set;}
        public Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct)=>
            Task.FromResult(new ImageWorkerStatus(true,"test-model"));
        public Task<Result> EnsureEditableAsync(MediaEditActor actor,CancellationToken ct)=>
            Task.FromResult(Expired?Result.Fail(BlogErrors.Unauthorized("expired")):Result.Ok());
        public async Task<Result<(byte[] Bytes,string Model)>> RemoveAsync(
            byte[] bytes,
            string contentType,
            CancellationToken ct)
        {
            if(Block)
            {
                if(Interlocked.Increment(ref _entered)==2)BothEntered.TrySetResult(true);
                await Release.Task.WaitAsync(ct);
            }
            using var source=SKBitmap.Decode(bytes);InputShape=(source.Width,source.Height);
            using var bitmap=new SKBitmap(source.Width,source.Height,SKColorType.Rgba8888,SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);
            using var image=SKImage.FromBitmap(bitmap);using var encoded=image.Encode(SKEncodedImageFormat.Png,100);
            return Result<(byte[] Bytes,string Model)>.Ok((encoded.ToArray(),Model));
        }
    }
}
