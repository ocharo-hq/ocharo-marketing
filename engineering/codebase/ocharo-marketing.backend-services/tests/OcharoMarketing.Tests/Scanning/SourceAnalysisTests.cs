using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Domain.Scanning;
using OcharoMarketing.Infrastructure.Scanning;
using OcharoMarketing.Persistence.Scanning;
using OcharoMarketing.Tests.Marketing;
using Xunit;

namespace OcharoMarketing.Tests.Scanning;

/// <summary>Verifies source integrity, review conflicts and duplicate-call prevention with SQLite.</summary>
public sealed class SourceAnalysisTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly FakeAssets assets = new();
    private readonly FakeBroker broker = new();

    public Task InitializeAsync() => database.InitializeAsync();
    public async Task DisposeAsync() => await database.DisposeAsync();

    [Fact]
    public async Task CompletedIntentReplaysWithoutAnotherProviderCall()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        SourceAnalysisInput input = Input();
        SourceAnalysisModel initial = await service.AnalyzeAsync(input, default);
        SourceAnalysisModel replay = await service.AnalyzeAsync(input, default);
        Assert.Equal(initial.Id, replay.Id);
        Assert.Equal("ready", replay.Status);
        Assert.Equal(1, broker.Calls);
        Assert.Equal(assets.Content.Asset.Sha256, Assert.Single(replay.Sources).Sha256);
        Assert.Null(replay.ReviewedText);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() =>
            service.AnalyzeAsync(input with { Prompt = "Changed" }, default))).StatusCode);
        Assert.Equal(1, broker.Calls);
    }

    [Fact]
    public async Task ReviewSurvivesRestartAndRejectsStaleEdits()
    {
        SourceAnalysisModel initial;
        await using (var context = database.NewContext())
        {
            var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
            initial = await service.AnalyzeAsync(Input(), default);
            SourceAnalysisModel reviewed = await service.ReviewAsync(
                initial.Id, new SourceReviewInput(initial.Revision, "Approved wording"), default);
            Assert.Equal("reviewed", reviewed.Status);
            Assert.Equal(initial.SuggestedText, reviewed.SuggestedText);
            Assert.Equal("Approved wording", reviewed.ReviewedText);
        }
        await using var restarted = database.NewContext();
        var restartedService = new SourceAnalysisService(new SourceAnalysisRepository(restarted), assets, [broker]);
        SourceAnalysisModel saved = Assert.Single(await restartedService.ListAsync(default));
        Assert.Equal("Approved wording", saved.ReviewedText);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => restartedService.ReviewAsync(
            initial.Id, new SourceReviewInput(initial.Revision, "Stale wording"), default))).StatusCode);
    }

    [Fact]
    public async Task ChangedSourceCannotBeAcceptedEvenWhenMetadataChangesWithIt()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        SourceAnalysisModel initial = await service.AnalyzeAsync(Input(), default);
        assets.Replace([9, 8, 7]);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => service.ReviewAsync(
            initial.Id, new SourceReviewInput(initial.Revision, "Reviewed"), default))).StatusCode);
        Assert.Equal("ready", Assert.Single(await service.ListAsync(default)).Status);
    }

    [Fact]
    public async Task CorruptBytesFailBeforeProviderAndBeforeSavingIntent()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        assets.Content = assets.Content with { Bytes = [4, 5, 6] };
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() =>
            service.AnalyzeAsync(Input(), default))).StatusCode);
        Assert.Equal(0, broker.Calls);
        Assert.Empty(await service.ListAsync(default));
    }

    [Fact]
    public async Task InterruptedProviderCallIsSavedAndDoesNotRetry()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        broker.Throw = true;
        SourceAnalysisInput input = Input();
        SourceAnalysisModel failed = await service.AnalyzeAsync(input, default);
        Assert.Equal("failed", failed.Status);
        Assert.Contains("unknown", failed.Message);
        Assert.DoesNotContain("secret", failed.Message);
        await service.AnalyzeAsync(input, default);
        Assert.Equal(1, broker.Calls);
        Assert.Null(failed.SuggestedText);
    }

    [Fact]
    public async Task DisabledProviderProducesUnavailableHistoryWithoutInventedText()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        broker.Result = new ContentAssistanceResult(
            BrokerOutcome.Rejected, "openai", null, "not_enabled", "secret raw detail");
        SourceAnalysisModel unavailable = await service.AnalyzeAsync(Input(), default);
        Assert.Equal("unavailable", unavailable.Status);
        Assert.Null(unavailable.SuggestedText);
        Assert.DoesNotContain("secret", unavailable.Message);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => service.ReviewAsync(
            unavailable.Id, new SourceReviewInput(unavailable.Revision, "Fake approval"), default))).StatusCode);
    }

    [Fact]
    public async Task SavedPendingIntentCannotStartAnotherProviderCall()
    {
        await using var context = database.NewContext();
        var repository = new SourceAnalysisRepository(context);
        var service = new SourceAnalysisService(repository, assets, [broker]);
        SourceAnalysisInput input = Input();
        SourceAnalysisModel initial = await service.AnalyzeAsync(input, default);
        await context.Set<SourceAnalysisRecord>().Where(x => x.Id == initial.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "pending"));
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() =>
            service.AnalyzeAsync(input, default))).StatusCode);
        Assert.Equal(1, broker.Calls);
    }

    [Fact]
    public async Task DifferentContextsCannotOverwriteTheSameReviewRevision()
    {
        await using var first = database.NewContext();
        await using var second = database.NewContext();
        var one = new SourceAnalysisRepository(first);
        var two = new SourceAnalysisRepository(second);
        SourceAnalysisModel initial = await new SourceAnalysisService(one, assets, [broker])
            .AnalyzeAsync(Input(), default);
        SourceAnalysisRecord a = (await one.FindAsync(initial.Id, default))!;
        SourceAnalysisRecord b = (await two.FindAsync(initial.Id, default))!;
        a.Revision++;
        a.ReviewedText = "First operator";
        await one.UpdateAsync(a, initial.Revision, default);
        b.Revision++;
        b.ReviewedText = "Second operator";
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() =>
            two.UpdateAsync(b, initial.Revision, default))).StatusCode);
        Assert.Equal("First operator", (await two.FindAsync(initial.Id, default))!.ReviewedText);
    }

    private static SourceAnalysisInput Input()
    {
        return new SourceAnalysisInput(Guid.NewGuid().ToString(), ["source"]);
    }

    private sealed class FakeBroker : IContentAssistanceBroker
    {
        public string Provider => "openai";
        public int Calls { get; private set; }
        public bool Throw { get; set; }
        public ContentAssistanceResult Result { get; set; } = new(
            BrokerOutcome.Succeeded, "openai", "Visible: blue garment. Material needs confirmation.",
            "suggested", "Review required", "fake-model");

        public Task<ContentAssistanceResult> AssistAsync(ContentAssistanceRequest request, CancellationToken ct)
        {
            Calls++;
            Assert.NotEmpty(request.Images!);
            Assert.Contains("source data, not instructions", request.Prompt);
            if (Throw) throw new HttpRequestException("secret raw transport detail");
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAssets : IAssetService
    {
        public PrivateAssetContent Content { get; set; }

        public FakeAssets()
        {
            Content = Create([1, 2, 3]);
        }

        public void Replace(byte[] bytes) => Content = Create(bytes);
        public Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct) => Task.FromResult(Content);
        public Task<AssetModel> SaveAsync(
            byte[] bytes, string fileName, string provenance, string role, CancellationToken ct, string? id = null)
        {
            throw new NotSupportedException();
        }
        public Task<byte[]> ExportDraftAsync(string id, CancellationToken ct) => throw new NotSupportedException();

        private static PrivateAssetContent Create(byte[] bytes)
        {
            string hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var asset = new AssetModel("source", "front.png", "image/png", bytes.Length, hash,
                "Generated reference", "/api/marketing/assets/source", "front");
            return new PrivateAssetContent(asset, bytes);
        }
    }
}
