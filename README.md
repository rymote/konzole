# Rymote.Konzole

Structured logging provider for `Microsoft.Extensions.Logging` with rich console output and pluggable sinks. Built on `System.Threading.Channels`: every sink owns a bounded background queue, so a slow Discord webhook never blocks a fast console write — or your application.

## Install

```bash
dotnet add package Rymote.Konzole
```

## Quick start

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Extensions;
using Rymote.Konzole.Sinks.Files;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddKonzole(konzole =>
{
    konzole.AddConsoleSink(options =>
    {
        options.UseColors = true;
        options.UseEmojis = true;
    });

    konzole.AddFileSink("logs/app.log", options =>
    {
        options.RollingPolicy = FileRollingPolicy.SizeOnly;
        options.MaxFileSize = 10 * 1024 * 1024;
        options.MaxFiles = 5;
    });

    konzole.AddDiscordSink("https://discord.com/api/webhooks/...", options =>
    {
        options.MinimumLevel = LogLevel.Warning;
    });
});

using IHost host = builder.Build();
ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogSuccess("Konzole is up");
```

## Built-in sinks

| Sink         | Default formatter   | Notes |
|--------------|---------------------|-------|
| `ConsoleSink`| `ConsoleFormatter`  | UTF-8 emoji icons with bracket fallback; Error/Critical → stderr. |
| `FileSink`   | `JsonFormatter`     | Size-only, date-only, or date-then-size rolling policies. |
| `RemoteSink` | `JsonFormatter`     | Batches and POSTs to any HTTP endpoint; bearer-token auth optional. |
| `DiscordSink`| `DiscordFormatter`  | Single-entry webhook posts with optional embed payload. |
| `SlackSink`  | `SlackFormatter`    | Single-entry webhook posts with optional attachments. |

All HTTP sinks use `IHttpClientFactory` under the hood with bounded retry + exponential backoff + `Retry-After` for 429s.

## Custom-level helpers

Konzole adds tagged log helpers that ride on top of the standard MEL levels by attaching a `KonzoleTag` scope. Konzole sinks render the tag (icon, color); non-Konzole sinks see a normal `LogInformation` call with a `KonzoleScopeState` scope they can safely ignore.

```csharp
logger.LogStart("starting work");
logger.LogPending("waiting for upstream");
logger.LogSuccess("completed");
logger.LogComplete("post-processing finished");
logger.LogNote("hint: tune the batch size");
logger.LogPause("paused for backoff");
logger.LogWatch("watching {Resource}", resourceName);
logger.LogFatal(exception, "unrecoverable: {Reason}", reason);
```

Mapping:

| Helper       | MEL level     | Tag                  |
|--------------|---------------|----------------------|
| `LogStart`   | `Information` | `KonzoleTag.Start`   |
| `LogPending` | `Information` | `KonzoleTag.Pending` |
| `LogSuccess` | `Information` | `KonzoleTag.Success` |
| `LogComplete`| `Information` | `KonzoleTag.Complete`|
| `LogNote`    | `Information` | `KonzoleTag.Note`    |
| `LogPause`   | `Information` | `KonzoleTag.Pause`   |
| `LogWatch`   | `Debug`       | `KonzoleTag.Watch`   |
| `LogFatal`   | `Critical`    | _(none — Critical already is fatal)_ |

## Writing a custom sink

```csharp
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

public sealed class StdoutEchoSink : SinkBase<StdoutEchoSinkOptions>
{
    public StdoutEchoSink(StdoutEchoSinkOptions options) : base(options) { }

    public override string Name => "StdoutEcho";

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(useEmojis: false);

    protected override ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
            Console.Out.WriteLine(Formatter.Format(entry, FormatterContext));
        return ValueTask.CompletedTask;
    }
}

public sealed class StdoutEchoSinkOptions : SinkOptionsBase { }
```

Register via:

```csharp
builder.Logging.AddKonzole(konzole => konzole.AddSink(new StdoutEchoSink(new StdoutEchoSinkOptions())));
```

## Configuration knobs (per sink)

All sinks inherit from `SinkOptionsBase`:

- `MinimumLevel` (`LogLevel.Information`)
- `ShowTimestamp`, `TimestampFormat`, `ShowCategory`, `ShowEventId`, `ShowScope`, `ShowException`
- `MaxMessageLength` (`4000`)
- `MaxQueueSize` (`10_000`) — bounded channel capacity; DropOldest on overflow
- `ShutdownTimeout` (`5s`) — `DisposeAsync` drain bound
- `Formatter` — override the default formatter

HTTP sinks (`RemoteSink`, `DiscordSink`, `SlackSink`) add:

- `MaxRetryAttempts` (`3`), `BaseRetryDelay` (`1s`), `MaxRetryDelay` (`30s`)
- `BatchSize` (`100` for `Remote`, `1` for Discord/Slack)
- `HttpClientName` — the name passed to `IHttpClientFactory.CreateClient`
- `RequestTimeout` (`30s`)

## Diagnostics

Sink failures are reported via the static `KonzoleDiagnostics.SinkError` event. If no subscriber is attached, Konzole writes a one-line summary to `stderr` (rate-limited to once per minute per sink).

```csharp
using Rymote.Konzole.Diagnostics;

KonzoleDiagnostics.SinkError += (sender, eventArgs) =>
{
    metricsCollector.Increment("logging.sink.error", new { sink = eventArgs.SinkName });
};
```

## Migrating from 0.1.x

- `KonzoleLogLevel` removed. Use `Microsoft.Extensions.Logging.LogLevel` for severity and `Rymote.Konzole.Models.KonzoleTag` for visual tagging.
- `LogEntry.Level` is now `LogLevel`; `LogEntry.Timestamp` is now `DateTimeOffset`; `Color` is gone; `Tag` is added.
- `ILogFormatter.Format(LogEntry)` → `ILogFormatter.Format(LogEntry, FormatterContext)`.
- `ISink.WriteAsync(LogEntry)` → `ISink.TryEnqueue(LogEntry)` (non-blocking); `FlushAsync` takes `CancellationToken`.
- Removed extension methods: `AddKonzoleStdout`, `AddKonzoleFile`, `AddKonzoleRemote`, `AddKonzoleDiscord`, `AddKonzoleSlack` — use `AddKonzole(builder => builder.Add…Sink(...))`.
- Removed: `LogSuccessWithData<T>`, `LogErrorWithData<T>` (Serilog `{@Data}` syntax, never worked under MEL).

## License

MIT
