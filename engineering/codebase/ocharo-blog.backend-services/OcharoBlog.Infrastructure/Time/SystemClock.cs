using OcharoBlog.Application.Abstractions;

namespace OcharoBlog.Infrastructure.Time;

/// <summary>Wall-clock implementation of <see cref="IClock"/>.</summary>
internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
