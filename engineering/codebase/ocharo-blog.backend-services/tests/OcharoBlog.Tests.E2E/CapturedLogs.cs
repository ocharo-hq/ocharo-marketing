using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoBlog.Api.RuntimeConfig.Settings;

namespace OcharoBlog.Tests.E2E;

/// <summary>Keeps every warning the host logs, so a test can count the startup warnings.</summary>
internal sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> warnings = new();

    public IReadOnlyCollection<string> Warnings => warnings.ToArray();

    public ILogger CreateLogger(string categoryName) => new Logger(warnings);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) warnings.Enqueue(formatter(state, exception));
        }
    }
}
