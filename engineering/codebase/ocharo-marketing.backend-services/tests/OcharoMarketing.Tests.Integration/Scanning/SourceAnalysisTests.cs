using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using AwesomeAssertions;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Domain.Scanning;
using OcharoMarketing.Infrastructure.Scanning;
using OcharoMarketing.Persistence.Scanning;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Scanning;

/// <summary>Verifies source integrity, review conflicts and duplicate-call prevention with SQLite.</summary>
public sealed class SourceAnalysisTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly FakeAssets assets = new();
    private readonly FakeBroker broker = new();

    public Task InitializeAsync() => database.InitializeAsync();
    public async Task DisposeAsync() => await database.DisposeAsync();

    [Fact]
    public async Task Analyze_ShouldReplayWithoutAnotherProviderCall_WhenTheIntentCompleted()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        SourceAnalysisInput input = Input();
        SourceAnalysisModel initial = await service.AnalyzeAsync(input, default);
        SourceAnalysisModel replay = await service.AnalyzeAsync(input, default);
        replay.Id.Should().Be(initial.Id);
        replay.Status.Should().Be("ready");
        broker.Calls.Should().Be(1);
        replay.Sources.Should().ContainSingle().Which.Sha256.Should().Be(assets.Content.Asset.Sha256);
        replay.ReviewedText.Should().BeNull();
        (ErrorStatus.Of((await FluentActions.Awaiting(() =>
            service
                .AnalyzeAsync(input with { Prompt = "Changed" }, default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(409);
        broker.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Review_ShouldSurviveARestartAndRefuseStaleEdits()
    {
        SourceAnalysisModel initial;
        await using (var context = database.NewContext())
        {
            var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
            initial = await service.AnalyzeAsync(Input(), default);
            SourceAnalysisModel reviewed = await service.ReviewAsync(
                initial.Id, new SourceReviewInput(initial.Revision, "Approved wording"), default);
            reviewed.Status.Should().Be("reviewed");
            reviewed.SuggestedText.Should().Be(initial.SuggestedText);
            reviewed.ReviewedText.Should().Be("Approved wording");
        }
        await using var restarted = database.NewContext();
        var restartedService = new SourceAnalysisService(new SourceAnalysisRepository(restarted), assets, [broker]);
        SourceAnalysisModel saved = (await restartedService.ListAsync(default)).Should().ContainSingle().Which;
        saved.ReviewedText.Should().Be("Approved wording");
        (ErrorStatus.Of((await FluentActions.Awaiting(() => restartedService.ReviewAsync(
            initial.Id,
            new SourceReviewInput(initial.Revision, "Stale wording"),
            default)).Should().ThrowExactlyAsync<AppException>()).Which)).Should().Be(409);
    }

    [Fact]
    public async Task Analyze_ShouldRefuseAChangedSource_WhenItsMetadataChangesWithIt()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        SourceAnalysisModel initial = await service.AnalyzeAsync(Input(), default);
        assets.Replace([9, 8, 7]);
        (ErrorStatus.Of((await FluentActions.Awaiting(() => service.ReviewAsync(
            initial.Id,
            new SourceReviewInput(initial.Revision, "Reviewed"),
            default)).Should().ThrowExactlyAsync<AppException>()).Which)).Should().Be(409);
        ((await service.ListAsync(default)).Should().ContainSingle().Which.Status).Should().Be("ready");
    }

    [Fact]
    public async Task Analyze_ShouldFailBeforeTheProviderAndTheIntent_WhenBytesAreCorrupt()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        assets.Content = assets.Content with { Bytes = [4, 5, 6] };
        ErrorStatus.Of((await FluentActions.Awaiting(() =>
            service.AnalyzeAsync(Input(), default)).Should().ThrowExactlyAsync<AppException>()).Which).Should().Be(409);
        broker.Calls.Should().Be(0);
        (await service.ListAsync(default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Analyze_ShouldSaveWithoutRetry_WhenTheProviderCallIsInterrupted()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        broker.Throw = true;
        SourceAnalysisInput input = Input();
        SourceAnalysisModel failed = await service.AnalyzeAsync(input, default);
        failed.Status.Should().Be("failed");
        failed.Message.Should().Contain("unknown");
        failed.Message.Should().NotContain("secret");
        await service.AnalyzeAsync(input, default);
        broker.Calls.Should().Be(1);
        failed.SuggestedText.Should().BeNull();
    }

    [Fact]
    public async Task Analyze_ShouldRecordUnavailableWithoutInventedText_WhenTheProviderIsDisabled()
    {
        await using var context = database.NewContext();
        var service = new SourceAnalysisService(new SourceAnalysisRepository(context), assets, [broker]);
        broker.Result = new ContentAssistanceResult(
            BrokerOutcome.Rejected, "openai", null, "not_enabled", "secret raw detail");
        SourceAnalysisModel unavailable = await service.AnalyzeAsync(Input(), default);
        unavailable.Status.Should().Be("unavailable");
        unavailable.SuggestedText.Should().BeNull();
        unavailable.Message.Should().NotContain("secret");
        (ErrorStatus.Of((await FluentActions.Awaiting(() => service.ReviewAsync(
            unavailable.Id,
            new SourceReviewInput(unavailable.Revision, "Fake approval"),
            default)).Should().ThrowExactlyAsync<AppException>()).Which)).Should().Be(409);
    }

    [Fact]
    public async Task Analyze_ShouldStartNoProviderCall_WhenAPendingIntentIsSaved()
    {
        await using var context = database.NewContext();
        var repository = new SourceAnalysisRepository(context);
        var service = new SourceAnalysisService(repository, assets, [broker]);
        SourceAnalysisInput input = Input();
        SourceAnalysisModel initial = await service.AnalyzeAsync(input, default);
        await context.Set<SourceAnalysisRecord>().Where(x => x.Id == initial.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "pending"));
        ErrorStatus.Of((await FluentActions.Awaiting(() =>
            service.AnalyzeAsync(input, default)).Should().ThrowExactlyAsync<AppException>()).Which).Should().Be(409);
        broker.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Review_ShouldNotOverwriteTheSameRevision_WhenContextsDiffer()
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
        ErrorStatus.Of((await FluentActions.Awaiting(() =>
            two
                .UpdateAsync(b, initial.Revision, default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which)
                .Should()
                .Be(409);
        ((await two.FindAsync(initial.Id, default))!.ReviewedText).Should().Be("First operator");
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
            request.Images!.Should().NotBeEmpty();
            request.Prompt.Should().Contain("source data, not instructions");
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
        public Task<AssetContent> OpenAsync(string id, CancellationToken ct) =>
            Task.FromResult(new AssetContent(Content.Asset, null, Content.Bytes));
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
