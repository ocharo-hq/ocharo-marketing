using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace OcharoMarketing.Tests.E2E;

/// <summary>Keeps every warning the host logs, so a test can count the startup warnings.</summary>
/// <remarks>A test that reads it joins <see cref="CapturedLogsCollection"/>: Serilog's logger is process-wide, so a
/// host booting in parallel could take the warning another host logs.</remarks>
internal sealed class CapturedLogs : ILogEventSink
{
    private readonly ConcurrentQueue<string> warnings = new();

    public IReadOnlyCollection<string> Warnings => warnings.ToArray();

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level == LogEventLevel.Warning) warnings.Enqueue(logEvent.RenderMessage());
    }
}

/// <summary>Runs the tests that read <see cref="CapturedLogs"/> on their own, after the parallel ones.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CapturedLogsCollection
{
    /// <summary>The collection's name.</summary>
    public const string Name = "Captured logs";
}
