<div align="center">
    <a href="https://github.com/rymote/konzole"><img src="https://github.com/rymote/konzole/blob/master/.github/rymote-konzole-cover.png" alt="rymote/konzole" /></a>
</div>
<br />

<div align="center">
  Rymote.Konzole - Structured logging provider for .NET with rich console output and pluggable sinks
</div>

<div align="center">
  <sub>
    Brought to you by
    <a href="https://github.com/jovanivanovic">@jovanivanovic</a>,
    <a href="https://github.com/rymote">@rymote</a>
  </sub>
</div>

## Overview

Rymote.Konzole is a structured logging provider for `Microsoft.Extensions.Logging` with rich console output and pluggable sinks. Built on `System.Threading.Channels`: every sink owns a bounded background queue, so a slow Discord webhook never blocks a fast console write — or your application.

## Features

- **Drop-in `Microsoft.Extensions.Logging` provider** — registers via `builder.Logging.AddKonzole(...)` and integrates with the standard `ILogger<T>` surface.
- **Bounded per-sink channels** — every sink runs its own `System.Threading.Channels` background worker with a configurable capacity and a DropOldest overflow policy.
- **Rich multi-color console output** — UTF-8 emoji icons, bracketed level/tag labels (`[INFO]`, `[ERROR]`, `[SUCCESS]`), per-segment coloring (timestamp dim, category cyan, message in level color), and `stderr` routing for `Error`/`Critical`. Independent toggles for the emoji icon and the bracket label.
- **Inline style markup** — embed `[red]`, `[bold]`, `[bg:yellow black]`, `[#ff8800]`, `[color:208]`, etc. directly in log messages. `ConsoleSink` emits ANSI escape sequences; non-console sinks (file/HTTP/Discord/Slack) strip the markup cleanly.
- **Built-in sinks** — console, file (size/date/date+size rolling), remote HTTP (batched), Discord webhook, Slack webhook.
- **Custom-level helpers** — `LogStart`, `LogPending`, `LogSuccess`, `LogComplete`, `LogNote`, `LogPause`, `LogWatch`, `LogFatal` ride on top of standard MEL levels via a `KonzoleTag` scope.
- **Pluggable formatters and sinks** — implement `ILogFormatter` or extend `SinkBase<TOptions>` to ship your own.
- **HTTP transport hardening** — `IHttpClientFactory`-backed sinks with bounded retry, exponential backoff, and `Retry-After` handling for 429s.
- **Diagnostic event surface** — `KonzoleDiagnostics.SinkError` surfaces sink failures to your metrics pipeline; falls back to a rate-limited `stderr` message when nobody is listening.

## Installation

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

## Inline style syntax

Embed style markup directly in log messages using a BBCode-style grammar. The `ConsoleSink` converts the markup to ANSI escape sequences; every other sink (file, remote HTTP, Discord, Slack) strips the markup so non-terminal consumers receive plain text.

```csharp
logger.LogInformation("User [bold bright_cyan]{Email}[/] logged in from [yellow]{Ip}[/]", email, ip);
logger.LogWarning("Rate limit [bold]87%[/] of quota for [magenta]POST /api/orders[/]");
logger.LogError("Database call failed: [bold red]Connection refused[/] after [yellow]3 retries[/]");
```

### Grammar

```
[<style-list>]content[/]
[[              → literal '['
```

`<style-list>` is one or more whitespace-separated tokens (case-insensitive):

| Category | Tokens |
|----------|--------|
| Named foreground (16) | `black`, `red`, `green`, `yellow`, `blue`, `magenta`, `cyan`, `white`, `bright_black`, …, `bright_white` |
| Named background | `bg:<any foreground token>` (e.g. `bg:yellow`, `bg:bright_red`) |
| 256-color palette | `color:N` (foreground, N=0-255), `bg:color:N` (background) |
| Truecolor hex | `#rrggbb` (foreground), `bg:#rrggbb` (background) |
| Text decoration | `bold`, `italic`, `underline`, `dim`, `reverse` |

Combine freely in a single tag: `[bold bright_red bg:black]critical[/]`, `[italic #7c3aed underline]heads up[/]`.

### Open / close / leniency

- `[/]` closes the most recently opened tag (LIFO).
- Tags compose: foreground/background of the inner layer override the outer; decoration flags OR together.
- Any `[xxx]` whose body doesn't parse as a valid style list passes through as literal text (`[Login]`, `[2026-05-21]`, `[App.Service]` render unchanged).
- `[[` escapes a literal `[` for the rare case you need `[red]` rendered as text.

### Public API

```csharp
using Rymote.Konzole.Styling;

string ansi    = StyleMarkup.ToAnsi("[red]error[/]");     // for direct terminal output
string stripped = StyleMarkup.Strip("[red]error[/]");      // for plain-text targets — returns "error"
```

## ConsoleSink output controls

The `ConsoleSink` line layout is composed of named segments — icon, level label, timestamp, category, scope, message, properties, exception — each with its own default style. Override any segment via `ConsoleSinkOptions.SegmentStyles`, or toggle the leading elements:

```csharp
konzole.AddConsoleSink(options =>
{
    options.UseColors = true;          // master color toggle
    options.UseEmojis = true;          // emoji icon when terminal supports UTF-8
    options.ShowIcon = true;           // entire icon segment (emoji + bracket fallback)
    options.ShowLevelLabel = true;     // bracketed [INFO]/[ERROR]/[SUCCESS] label

    options.SegmentStyles = new Dictionary<ConsoleSegment, AnsiStyle>
    {
        [ConsoleSegment.Timestamp] = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Yellow) }
    };
});
```

Default segment styles are tuned for a dark terminal — dim timestamps, cyan category, bold-red level labels for errors, bright-white property values over dim keys, and so on. Per-level emphasis is keyed off `ConsoleSegment.Message`, `MessageWarning`, and `MessageError` so warnings and errors stand out without overriding routine messages.

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

`ConsoleSink` adds:

- `UseColors` (`true`), `UseEmojis` (`true`)
- `ShowIcon` (`true`), `ShowLevelLabel` (`true`)
- `LevelColors` / `TagColors` / `CriticalBackgroundColor` — the dynamic per-entry color picked up by `Icon` and `LevelLabel` segments
- `SegmentStyles` — full per-segment style overrides, merged on top of the defaults

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

## Support the project

If Konzole has helped you ship faster, please consider supporting ongoing development:

- [Patreon](https://www.patreon.com/rymote)
- [Open Collective](https://opencollective.com/rymote)

## License

This project is licensed under the BSD 3-Clause License — see [LICENSE.md](./LICENSE.md) for details.
