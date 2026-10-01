using System.Collections.Concurrent;
using OcharoMarketing.Application.Marketing.Models;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Reports only checks performed by this process; monitoring GET never creates a heartbeat.</summary>
public sealed class WorkerHealthService(TimeProvider time)
{
    private readonly ConcurrentDictionary<string, WorkerHealthModel> workers = new();
    private static readonly (string Id, int PollSeconds)[] Definitions = [("automation", 300), ("publication", 2), ("generation", 1)];

    public void Started(string worker)
    {
        var now = time.GetUtcNow();
        workers[worker] = new(worker, "running", now, now, null, null, Definitions.Single(x => x.Id == worker).PollSeconds);
    }

    public void Completed(string worker) => workers.AddOrUpdate(worker,
        _ => new(worker, "healthy", time.GetUtcNow(), time.GetUtcNow(), time.GetUtcNow(), null, Definitions.Single(x => x.Id == worker).PollSeconds),
        (_, old) => old with { Status = "healthy", CheckedAt = time.GetUtcNow(), LastSuccessAt = time.GetUtcNow(), LastError = null });

    public void Failed(string worker) => workers.AddOrUpdate(worker,
        _ => new(worker, "failed", time.GetUtcNow(), time.GetUtcNow(), null, "Worker pass failed; inspect local logs.", Definitions.Single(x => x.Id == worker).PollSeconds),
        (_, old) => old with { Status = "failed", CheckedAt = time.GetUtcNow(), LastError = "Worker pass failed; inspect local logs." });

    public WorkerHealthModel[] Read() => Definitions.Select(definition =>
    {
        if (!workers.TryGetValue(definition.Id, out var current))
            return new WorkerHealthModel(definition.Id, "unobserved", null, null, null, null, definition.PollSeconds);
        // Generation can render for up to fifteen minutes; a dispatch can wait on a provider for five.
        var timeout = definition.Id == "automation" ? 660 : definition.Id == "generation" ? 960 : 360;
        return current.CheckedAt is { } check && time.GetUtcNow() - check > TimeSpan.FromSeconds(timeout)
            ? current with { Status = "stale" } : current;
    }).ToArray();
}
