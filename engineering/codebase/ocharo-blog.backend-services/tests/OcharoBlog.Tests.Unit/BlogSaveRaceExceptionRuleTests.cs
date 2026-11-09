using Microsoft.EntityFrameworkCore;
using Npgsql;
using OcharoBlog.Persistence.Blog.Errors;

namespace OcharoBlog.Tests.Unit;

/// <summary>A save that lost a race with another save is the 409 the editor reloads after; any other failure is
/// left to the be sdk's rules.</summary>
public sealed class BlogSaveRaceExceptionRuleTests
{
    private readonly BlogSaveRaceExceptionRule rule = new();

    [Fact]
    public void TryMap_ShouldReturnSaveRaced_WhenAConcurrencyTokenMoved()
    {
        var error = rule.TryMap(new DbUpdateConcurrencyException("The row changed."));

        error.Should().NotBeNull();
        error!.Type.Should().Be(AppErrorType.Conflict);
        error.Message.Should().Be("Another save is finishing. Reload and retry.");
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    [InlineData(PostgresErrorCodes.DeadlockDetected)]
    [InlineData(PostgresErrorCodes.LockNotAvailable)]
    public void TryMap_ShouldReturnSaveRaced_WhenPostgresRefusedTheSaveForARace(string state)
    {
        var refused = new PostgresException("refused", "ERROR", "ERROR", state);

        rule.TryMap(new DbUpdateException("Save failed.", refused))!.Type.Should().Be(AppErrorType.Conflict);
        rule.TryMap(refused)!.Type.Should().Be(AppErrorType.Conflict);
    }

    [Fact]
    public void TryMap_ShouldReturnNone_WhenTheFailureIsNoRace()
    {
        var foreignKey = new PostgresException("refused", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation);

        rule.TryMap(new DbUpdateException("Save failed.", foreignKey)).Should().BeNull();
        rule.TryMap(new InvalidOperationException("Not a save.")).Should().BeNull();
    }
}
