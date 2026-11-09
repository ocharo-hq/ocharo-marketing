using AwesomeAssertions;
using Npgsql;
using OcharoMarketing.Api.Core.Errors;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using Xunit;

namespace OcharoMarketing.Tests.Unit.Core;

/// <summary>Protects the status and code each catalogue error answers with, as Marketing answered them before the
/// mediator, and the exceptions the host answers as client errors.</summary>
public sealed class MarketingErrorMappingTests
{
    private static readonly MarketingErrorHttpStatusCodeMapper Mapper = new();
    private static readonly MarketingExceptionMappingRule Rule = new();

    public static TheoryData<AppError, int, string> Catalogue => new()
    {
        { MarketingErrors.Invalid("x"), 400, "Validation" },
        { MarketingErrors.Unauthenticated("x"), 401, "Unauthorized" },
        { MarketingErrors.NotFound("x"), 404, "NotFound" },
        { MarketingErrors.Conflict("x"), 409, "Conflict" },
        { MarketingErrors.TooLarge("x"), 413, "PayloadTooLarge" },
        { MarketingErrors.UnsupportedMedia("x"), 415, "UnsupportedMediaType" },
        { MarketingErrors.Unprocessable("x"), 422, "Validation" },
        { MarketingErrors.Throttled("x"), 429, "TooManyRequests" },
        { MarketingErrors.Refused("x"), 502, "Unexpected" },
        { MarketingErrors.Unavailable("x"), 503, "Unexpected" },
    };

    [Theory]
    [MemberData(nameof(Catalogue))]
    public void ToStatusCode_ShouldKeepTheEarlierStatusAndCode_WhenTheErrorIsCatalogued(
        AppError error,
        int status,
        string code)
    {
        Mapper.ToStatusCode(error).Should().Be(status);
        var named = error.Metadata?.TryGetValue(MarketingErrors.CodeKey, out var value) == true
            ? (string)value!
            : error.Type.ToString();
        named.Should().Be(code);
        (error.Metadata?[MarketingErrors.TitleKey]).Should().Be(MarketingErrors.RejectedTitle);
    }

    [Fact]
    public void TryMap_ShouldAnswer422CardNeedsRevision_WhenTheRendererRefusesTheCard()
    {
        var error = Rule.TryMap(new CardRenderingException("Shorten the copy."));

        error.Should().NotBeNull();
        Mapper.ToStatusCode(error).Should().Be(422);
        error.Type.Should().Be(AppErrorType.Validation);
        error.Message.Should().Be("Shorten the copy.");
        (error.Metadata?[MarketingErrors.TitleKey]).Should().Be("Card needs revision");
    }

    [Fact]
    public void TryMap_ShouldAnswer400InvalidComposition_WhenAnArgumentIsRefused()
    {
        var error = Rule.TryMap(new ArgumentException("A private detail."));

        error.Should().NotBeNull();
        Mapper.ToStatusCode(error).Should().Be(400);
        error.Message.Should().Be(MarketingErrors.CompositionInvalidMessage);
        (error.Metadata?[MarketingErrors.TitleKey]).Should().Be("Invalid composition");
    }

    [Theory]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.LockNotAvailable)]
    public void TryMap_ShouldAnswer409WorkspaceBusy_WhenPostgresGivesUpOnALock(string sqlState)
    {
        var error = Rule.TryMap(new PostgresException("lock", "ERROR", "ERROR", sqlState));

        error.Should().NotBeNull();
        Mapper.ToStatusCode(error).Should().Be(409);
        error.Type.Should().Be(AppErrorType.Conflict);
        error.Message.Should().Be(MarketingErrors.WorkspaceBusyMessage);
        (error.Metadata?[MarketingErrors.TitleKey]).Should().Be("Workspace busy");
    }

    [Fact]
    public void TryMap_ShouldLeaveTheError_WhenTheExceptionIsNotMarketings()
    {
        (Rule.TryMap(new PostgresException("unique", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation)))
            .Should()
            .BeNull();
        (Rule.TryMap(new InvalidOperationException("x"))).Should().BeNull();
    }
}
