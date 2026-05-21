# Konzole Overhaul — Design

**Date:** 2026-05-17
**Status:** Approved for planning
**Target version:** `0.2.0` (pre-1.0; breaking changes accepted)

## 1. Goals

1. Eliminate the correctness bugs documented in the audit (broken `IsEnabled`, sync-over-async on every log call, lost `AsyncLocal` scope, broken FileSink rotation, unbounded retry, missed structured properties, magic-EventId collisions).
2. Replace the EventId-based custom-level routing with a clean scope-state-object tag carried via `AsyncLocal`.
3. Move logging dispatch off the calling thread onto a bounded channel + per-sink worker model so a slow sink never blocks the application or another sink.
4. Decouple formatters from sink options so formatters become pure `(entry, context)` functions reusable across sinks.
5. Bring the project to a publishable shape: NuGet metadata, README, ignored log artifacts, and an xUnit test project covering the behaviors that changed.
6. Apply the global one-OOP-per-file rule — every class, struct, enum, interface, record gets its own file.

## 2. Non-goals

- Backwards compatibility with the current `0.1.x` public API. This is pre-1.0.
- Reintroducing the `KonzoleLogLevel` enum.
- Persistent on-disk retry buffer for HTTP sinks (rejected during brainstorming as overkill for v0).
- A sample application project (rejected during brainstorming).
- Any feature flags or runtime toggles for "old vs. new" behavior.

## 3. Architecture & data flow

```
ILogger.Log<TState>(level, eventId, state, exception, formatter)
        │
        ▼
KonzoleLogger.Log                                ← cheap: builds LogEntry, no I/O
   ├── extracts properties (handles IReadOnlyList<KeyValuePair<string, object?>>)
   ├── reads KonzoleScopeState.Current (AsyncLocal) → Tag, TraceId, SpanId
   └── for each sink: sink.TryEnqueue(entry)    ← non-blocking, lock-free channel write
        │
        ▼ (per sink, independently)
ISink internal Channel<LogEntry>                 ← bounded, DropOldest, default 10_000
        │
        ▼
ISink background worker (one per sink)           ← started in sink ctor
   ├── awaits the internal channel
   ├── for sync sinks (Console, File): writes one entry at a time
   ├── for batching sinks (HTTP): drains up to BatchSize, sends a batch
   └── exceptions → KonzoleDiagnostics event (stderr fallback, rate-limited)

KonzoleLoggerProvider.DisposeAsync               ← graceful drain
   ├── completes all sinks' channels
   ├── awaits all sink workers with ShutdownTimeout (default 5s)
   └── disposes sinks
```

Key invariants:

- `KonzoleLogger.Log` is synchronous and never performs I/O. Cost = building the `LogEntry` record + one lock-free `TryWrite` per sink.
- There is **one channel per sink**, not a central dispatcher channel. This avoids a double-buffering layer and means slow sinks apply backpressure only to themselves (via DropOldest), never to fast sinks or to the caller.
- `KonzoleLogger.IsEnabled(level)` returns `true` if **any** registered sink would accept the level (i.e. `level >= sink.MinimumLevel`). This fixes the existing `_sinks.Any(sink => sink is ISink)` which always returns `true`.
- Scope state survives `await` boundaries (`AsyncLocal<KonzoleScopeState?>` replaces `[ThreadStatic] LogScope`).
- Provider implements both `IDisposable` (MEL contract) and `IAsyncDisposable`. The synchronous `Dispose` calls `DisposeAsync().AsTask().GetAwaiter().GetResult()` with the shutdown timeout — bounded sync-over-async only at process exit.
- Sink errors never propagate to the calling thread. They are surfaced via a static `KonzoleDiagnostics.SinkError` event; if no subscriber, the sink writes a one-line summary to `Console.Error` (rate-limited to once per minute per sink to avoid flooding).

## 4. Public API surface

### 4.1 Registration

```csharp
services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddKonzole(konzoleBuilder =>
    {
        konzoleBuilder
            .AddConsoleSink(consoleSinkOptions => consoleSinkOptions.UseColors = true)
            .AddFileSink("logs/app.log", fileSinkOptions =>
            {
                fileSinkOptions.RollingPolicy = FileRollingPolicy.SizeOnly;
                fileSinkOptions.MaxFileSize = 10 * 1024 * 1024;
                fileSinkOptions.MaxFiles = 5;
            })
            .AddDiscordSink("https://discord.com/api/webhooks/...", discordSinkOptions =>
            {
                discordSinkOptions.MinimumLevel = LogLevel.Warning;
            });
    });
});
```

Surviving entry points:

- `AddKonzole(this ILoggingBuilder builder, Action<KonzoleBuilder> configure)` — primary.
- `AddKonzole(this ILoggingBuilder builder)` — defaults to a single Console sink with colors and emojis.

Removed:

- `AddKonzoleStdout`, `AddKonzoleFile`, `AddKonzoleRemote`, `AddKonzoleDiscord`, `AddKonzoleSlack` — single-sink sugar that duplicates the builder.
- `AddKonzoleAll` (already removed in commit `6bca4aa`).

### 4.2 Hybrid DI for sinks

- `AddConsoleSink`, `AddFileSink` — constructed inline (no service dependencies). The builder appends a `ServiceDescriptor` that returns the pre-built instance.
- `AddDiscordSink`, `AddSlackSink`, `AddRemoteSink` — registered as `ServiceDescriptor.Describe(typeof(ISink), serviceProvider => new TSink(options, serviceProvider.GetRequiredService<IHttpClientFactory>()), ServiceLifetime.Singleton)`. The builder always calls `services.AddHttpClient()` during `Build()` (idempotent — internally uses `TryAdd`, so a host that has already wired up HttpClient is unaffected).
- `AddSink<TSink>(this KonzoleBuilder builder) where TSink : class, ISink` — registers `TSink` with the container; resolved via DI so it can take any constructor dependencies.
- `AddSink(this KonzoleBuilder builder, ISink instance)` — for already-constructed sinks (testing, advanced).

The provider receives sinks via `IEnumerable<ISink>` injection (standard MEL pattern), so all registration paths converge in DI.

### 4.3 Custom-level tag (KonzoleScopeState)

```csharp
public sealed class KonzoleScopeState
{
    public KonzoleTag? Tag { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}

public enum KonzoleTag
{
    Success,
    Pending,
    Complete,
    Note,
    Start,
    Pause,
    Watch
}
```

`KonzoleScopeState.Current` is an `AsyncLocal<KonzoleScopeState?>` exposed via a static property. Helper extension methods push a tagged scope and log at the appropriate MEL level:

```csharp
public static class KonzoleLoggerExtensions
{
    public static void LogSuccess(this ILogger logger, string message, params object?[] arguments)
    {
        using (logger.BeginScope(new KonzoleScopeState { Tag = KonzoleTag.Success }))
            logger.LogInformation(message, arguments);
    }

    // LogPending, LogComplete, LogNote, LogStart, LogPause — same shape, Information level.
    // LogWatch — Debug level.
    // LogFatal(exception, message, ...) — LogCritical, no tag (Critical is fatal already).
}
```

`KonzoleLogger.Log` reads `KonzoleScopeState.Current` and copies `Tag`/`TraceId`/`SpanId` onto the `LogEntry`. Non-Konzole sinks observing the scope see a `KonzoleScopeState` object — they can ignore it. Konzole formatters use `LogEntry.Tag` (when present) to pick icon/color; otherwise fall back to icon/color-by-level.

Removed:

- `LogSuccessWithData<T>`, `LogErrorWithData<T>` — Serilog `{@Data}` syntax that does not work under MEL's default formatter.

### 4.4 LogEntry shape

```csharp
public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    KonzoleTag? Tag,
    string Message,
    string? Category,
    EventId EventId,
    Exception? Exception,
    IReadOnlyDictionary<string, object?>? Properties,
    string? Scope,
    string? TraceId,
    string? SpanId);
```

- `Timestamp` is `DateTimeOffset` (was `DateTime`) — offset-preserving for cross-timezone deployments.
- `Level` is `Microsoft.Extensions.Logging.LogLevel`. The `KonzoleLogLevel` enum is removed.
- `Color` is removed from `LogEntry` — color is a presentation concern owned by the sink.
- Type is now an immutable `record` with init-only properties.

## 5. Formatter / sink decoupling

```csharp
public sealed class FormatterContext
{
    public bool ShowTimestamp { get; init; } = true;
    public string TimestampFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool ShowCategory { get; init; } = true;
    public bool ShowEventId { get; init; }
    public bool ShowScope { get; init; } = true;
    public bool ShowException { get; init; } = true;
    public int MaxMessageLength { get; init; } = 4000;
}

public interface ILogFormatter
{
    string Format(LogEntry entry, FormatterContext context);
}
```

- `FormatterBase` is removed. Helper utilities (timestamp/category/scope/exception append, category truncation) move to `internal static class FormatterHelpers` and operate on a `StringBuilder` + `FormatterContext`.
- Each sink builds a `FormatterContext` once at construction from its own options.
- Formatters are pure: same `(entry, context)` always produces the same output. Trivially testable.
- `MaxMessageLength` moves from per-sink option classes onto `FormatterContext` (and `SinkOptionsBase`) — eliminates the three duplicated declarations.

## 6. Sink redesign

### 6.1 ISink and SinkBase

```csharp
public interface ISink : IAsyncDisposable, IDisposable
{
    string Name { get; }
    LogLevel MinimumLevel { get; }
    void TryEnqueue(LogEntry entry);                                   // non-blocking; drops oldest if full
    ValueTask FlushAsync(CancellationToken cancellationToken);          // drain queue, wait for in-flight writes
}

public abstract class SinkBase<TOptions> : ISink
    where TOptions : SinkOptionsBase
{
    protected TOptions Options { get; }
    protected ILogFormatter Formatter { get; }
    protected FormatterContext FormatterContext { get; }

    private readonly Channel<LogEntry> _channel;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _shutdownTokenSource = new();

    protected SinkBase(TOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Formatter = options.Formatter ?? CreateDefaultFormatter();
        FormatterContext = BuildFormatterContext(options);

        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(Options.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        _workerTask = Task.Run(() => RunWorkerAsync(_shutdownTokenSource.Token));
    }

    public abstract string Name { get; }
    public LogLevel MinimumLevel => Options.MinimumLevel;

    public void TryEnqueue(LogEntry entry)
    {
        if (entry.Level < Options.MinimumLevel) return;
        _channel.Writer.TryWrite(entry);   // bounded + DropOldest → never blocks
    }

    public virtual ValueTask FlushAsync(CancellationToken cancellationToken) =>
        new(_channel.Reader.Completion.IsCompleted ? Task.CompletedTask : DrainOnceAsync(cancellationToken));

    protected abstract ILogFormatter CreateDefaultFormatter();
    protected abstract ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken);
    protected virtual int BatchSize => 1;

    private async Task RunWorkerAsync(CancellationToken cancellationToken) { /* drain loop calling WriteBatchAsync */ }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        try { await _workerTask.WaitAsync(Options.ShutdownTimeout); } catch (TimeoutException) { _shutdownTokenSource.Cancel(); }
        _shutdownTokenSource.Dispose();
    }

    void IDisposable.Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

Notes:

- `MaxQueueSize` and `ShutdownTimeout` move onto `SinkOptionsBase` so every sink inherits the bounded-channel + drain-timeout knobs.
- `WriteBatchAsync` always receives a list; sync sinks (Console, File) use `BatchSize = 1` and act on `batch[0]`. Batching sinks (HTTP) set `BatchSize` to whatever they want.
- `ConvertToLogLevel` is deleted (no more `KonzoleLogLevel`).
- The previous `WriteAsync(LogEntry)` and `Flush().Wait()` calls on the public surface are gone — `TryEnqueue` + `FlushAsync` are the only external entry points.

### 6.2 ConsoleSink

- No `Task.Run` wrapper — the sink worker is already off the caller's thread.
- No `Console.OutputEncoding = Encoding.UTF8` mutation in the constructor. The `ConsoleFormatter` checks `Console.OutputEncoding.CodePage == 65001` at format time and falls back to bracket icons otherwise. Users wanting emoji on non-UTF-8 hosts set the encoding themselves at startup.
- `lock` becomes `System.Threading.Lock` (net10).
- Error/Critical → `Console.Error`; everything else → `Console.Out`.
- Color routing: `entry.Tag` overrides level when set. Both maps live on `ConsoleSinkOptions`:
  - `IReadOnlyDictionary<KonzoleTag, ConsoleColor> TagColors { get; init; }` (defaults provided)
  - `IReadOnlyDictionary<LogLevel, ConsoleColor> LevelColors { get; init; }` (defaults provided)

### 6.3 FileSink

```csharp
public enum FileRollingPolicy
{
    SizeOnly,
    DateOnly,
    DateThenSize
}

internal interface IFileRollingStrategy
{
    string ResolveCurrentPath(string basePath, DateTimeOffset now);
    bool ShouldRoll(string currentPath, long currentSize, long pendingBytes, DateTimeOffset now);
    void Roll(string basePath, int maxFiles, DateTimeOffset now);
}
```

**`MaxFiles` semantics:** the total count of files kept on disk, *including* the active file. With `MaxFiles = 5` and `SizeOnly`, the on-disk set after several rotations is `app.log` + `app.1.log` + `app.2.log` + `app.3.log` + `app.4.log` — five files.

Strategies:

- `SizeOnlyRollingStrategy` — at roll:
  1. Dispose the writer.
  2. If `app.{maxFiles - 1}.log` exists, delete it.
  3. For `index` from `maxFiles - 2` down to `1`: rename `app.{index}.log` → `app.{index + 1}.log`.
  4. Rename `app.log` → `app.1.log`.
  5. Reopen `app.log` fresh (no append). Fixes the round-robin overwrite bug in the current implementation.
- `DateOnlyRollingStrategy` — active file is `app-yyyy-MM-dd.log`. Rolls when the current date differs from the active file's date. Cleanup deletes any `app-*.log` whose embedded date is older than `MaxFiles - 1` days (active counts as one).
- `DateThenSizeRollingStrategy` — composition: date-stamped active file (`app-yyyy-MM-dd.log`); when size cap is hit within a day, sub-rotate by renaming the active to `app-yyyy-MM-dd.1.log`, shifting any existing same-date files. Cleanup is date-based.

Other FileSink fixes:

- Internal `_messageQueue` and `_flushTimer` removed. The sink's own worker (inherited from `SinkBase`) drives writes. `FlushAsync` calls `_streamWriter.FlushAsync(ct)` directly.
- `Options.FlushInterval` (TimeSpan, default 1s) — when non-zero, the worker accumulates writes and flushes the underlying `StreamWriter` on this interval. When zero, flush after every write.
- `FilePath` default: `Path.Combine(AppContext.BaseDirectory, "logs", "konzole.log")` (survives working-directory changes).
- Default formatter remains `JsonFormatter`.

### 6.4 HTTP sinks (Remote, Discord, Slack)

Common base extends `SinkBase` and overrides `WriteBatchAsync` with retry logic:

```csharp
internal abstract class HttpSinkBase<TOptions> : SinkBase<TOptions>
    where TOptions : HttpSinkOptionsBase
{
    protected HttpClient HttpClient { get; }   // resolved via IHttpClientFactory at construction
    protected override int BatchSize => Options.BatchSize;
    protected abstract HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch);

    protected sealed override async ValueTask WriteBatchAsync(
        IReadOnlyList<LogEntry> batch,
        CancellationToken cancellationToken)
    {
        // retry loop: up to Options.MaxRetryAttempts, exponential backoff, honor Retry-After
    }
}
```

`HttpSinkOptionsBase` (extends `SinkOptionsBase`) adds:

- `int MaxRetryAttempts` (default 3)
- `TimeSpan BaseRetryDelay` (default 1s)
- `TimeSpan MaxRetryDelay` (default 30s)
- `int BatchSize` (default 100 for `RemoteSink`, 1 for Discord/Slack)
- `string HttpClientName` (default `"Konzole.<SinkName>"`)
- `TimeSpan RequestTimeout` (default 30s)

(`MaxQueueSize` and `ShutdownTimeout` are inherited from `SinkOptionsBase`.)

Retry rules:

- `429`: honor `Retry-After` header if present; else use exponential backoff.
- `5xx`, `408`, network failure: retry up to `MaxRetryAttempts` with exponential backoff (`min(BaseRetryDelay * 2^attempt, MaxRetryDelay)`).
- `4xx` (other): no retry; drop the batch and raise `KonzoleDiagnostics.SinkError`.
- On exhausted retries: drop the batch, raise `KonzoleDiagnostics.SinkError` with the dropped-entry count.

The DropOldest channel already in `SinkBase` provides the queue cap (`MaxQueueSize`). No extra internal channel inside HTTP sinks — the inherited one is the queue.

For `DiscordSink` / `SlackSink`, `BatchSize = 1` and `BuildRequest` builds a single-entry payload. The single-send semantics still flow through the retry path so 429s are still honored.

HttpClient lifecycle: resolved from `IHttpClientFactory` at construction using `Options.HttpClientName`. Sinks never call `HttpClient.Dispose()`.

### 6.5 File splits (one-OOP-per-file)

- `LogScope` (nested in `KonzoleLogger.cs`) — deleted. Replaced by `Models/KonzoleScopeState.cs`.
- `ExceptionJsonConverter` (nested in `JsonFormatter.cs`) — moved to `Formatters/Json/ExceptionJsonConverter.cs`, and wired into the `JsonFormatter` (was dead code, now active).
- `Models/LogLevel.cs` — deleted (enum removed entirely).
- `Models/KonzoleTag.cs` — new file for the `KonzoleTag` enum.
- `Models/LogEntry.cs` — replaced with the new record shape.
- `Models/LogIcon.cs` — kept, extended with a `GetIcon(KonzoleTag tag)` overload alongside the existing `GetIcon(LogLevel level)` (after renaming the existing overload's parameter type).

## 7. Testing

`Rymote.Konzole.Tests` (xUnit, net10.0):

```
Rymote.Konzole.Tests/
├── Formatters/
│   ├── ConsoleFormatterTests.cs       — output shape with/without tag, UTF-8 vs fallback
│   ├── JsonFormatterTests.cs          — schema; ExceptionJsonConverter wiring; null handling
│   ├── DiscordFormatterTests.cs       — markdown escape, length truncation
│   └── SlackFormatterTests.cs         — mrkdwn fields, channel field
├── Dispatch/
│   ├── KonzoleDispatcherTests.cs      — fan-out to multiple FakeSinks; per-sink ordering; drop-oldest under pressure
│   ├── KonzoleLoggerTests.cs          — IsEnabled honors level and per-sink minima; tag scope flows to LogEntry; AsyncLocal survives await
│   └── GracefulShutdownTests.cs       — DisposeAsync drains within ShutdownTimeout; entries flushed
├── Sinks/
│   ├── ConsoleSinkTests.cs            — tag color overrides level color; Error → stderr
│   ├── FileSinkRotationTests.cs       — SizeOnly rename shift; DateOnly day boundary; DateThenSize combo; MaxFiles cleanup
│   └── HttpSinkRetryTests.cs          — retry up to MaxRetryAttempts; honors 429 Retry-After; DropOldest when queue full; raises KonzoleDiagnostics on terminal failure
├── Configuration/
│   ├── KonzoleBuilderTests.cs         — Build() throws if no sinks; AddSink<T>() resolves via DI; AddHttpClient is added if absent
│   └── LoggerExtensionsTests.cs       — LogSuccess pushes correct KonzoleTag scope; LogFatal maps to Critical
└── Infrastructure/
    ├── FakeSink.cs                    — records entries, configurable per-entry delay
    ├── FakeHttpMessageHandler.cs      — programmable HTTP responses (200, 429+Retry-After, 5xx, timeout, network failure)
    └── FakeClock.cs                   — controllable `DateTimeOffset.UtcNow` for rotation/retry tests
```

Coverage targets focus on behaviors that changed or broke. No coverage-percentage goal.

## 8. Packaging

`Rymote.Konzole.csproj` adds:

```xml
<PropertyGroup>
    <PackageId>Rymote.Konzole</PackageId>
    <Version>0.2.0</Version>
    <Authors>Rymote</Authors>
    <Company>Rymote</Company>
    <Description>Structured logging provider for Microsoft.Extensions.Logging with rich console output and pluggable sinks (Console, File, Remote HTTP, Discord, Slack).</Description>
    <PackageTags>logging;console;microsoft-extensions-logging;discord;slack</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/rymote/konzole</PackageProjectUrl>
    <RepositoryUrl>https://github.com/rymote/konzole</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>

<ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
</ItemGroup>
```

`README.md` at repo root:

- One-paragraph pitch.
- Install (`dotnet add package Rymote.Konzole`).
- Quick-start registration snippet (the one in section 4.1).
- Built-in sinks table.
- Custom-level helpers (`LogSuccess`/`LogPending`/etc.) with an example.
- Writing a custom sink in ~15 lines.
- Configuration knobs per sink.
- Link to GitHub for issues/contributing.

## 9. Hygiene

- `.gitignore`: ensure `**/logs/`, `*.log`, `bin/`, `obj/` are excluded.
- Delete tracked log files: `Rymote.Konzole/logs/app.log`, `Rymote.Konzole/logs/production.log`.
- Verify `.github/workflows/publish-github.yml` and `publish-nuget.yml` tolerate the new metadata; adjust if they hard-code a version (they will be read during planning).
- Strip restating comments and dead code uncovered during the rewrite (e.g., `// Fallback if encoding change fails`, `// Register the provider with the configured sinks`).
- Replace single-letter loop variables (`for (int i = 0; ...)` in `ConsoleFormatter.cs:26` becomes `stringBuilder.Append(' ', 2)`).

## 10. Migration impact

This is a breaking release. Consumers of `0.1.x` must:

1. Replace `KonzoleLogLevel` references with `Microsoft.Extensions.Logging.LogLevel` + (optionally) `KonzoleTag`.
2. Drop calls to removed `AddKonzoleStdout` / `AddKonzoleFile` / `AddKonzoleRemote` / `AddKonzoleDiscord` / `AddKonzoleSlack` and use `AddKonzole(b => b.Add…Sink(...))`.
3. Drop calls to `LogSuccessWithData` / `LogErrorWithData` (removed).
4. If consuming `LogEntry` from a custom sink: it is now a record with `DateTimeOffset Timestamp`, `LogLevel Level` (not `KonzoleLogLevel`), and a new `KonzoleTag? Tag` property. `Color` is gone.
5. If implementing `ISink`: `WriteAsync` now returns `ValueTask` and takes `CancellationToken`. `FlushAsync` too.

The README's migration section will spell this out.

## 11. Open questions

None. All architectural choices were resolved during brainstorming (full overhaul; v0 breaking changes; scope-state-object tag; channel + per-sink workers; hybrid DI; bounded retry with exponential backoff + drop-oldest; rolling policy is `Both`, user picks; tests + packaging + README).
