# Konzole Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Commit policy:** This project's owner has a global rule against autonomous commits. Every step labelled "Commit" must be explicitly approved by the user before running the `git` command. If unsure, ask first.

**Goal:** Rewrite Rymote.Konzole's logger/dispatcher/sinks/formatters around a per-sink bounded-channel pipeline, replace the EventId-magic custom-level system with a scope-state tag, fix correctness bugs, ship NuGet metadata + tests + README.

**Architecture:** `KonzoleLogger.Log` builds a `LogEntry` and `TryEnqueue`s it into each `ISink`'s own bounded channel (DropOldest). Each sink owns a background worker that drains its channel — sync sinks write immediately, HTTP sinks batch + retry with exponential backoff. Custom levels (Success, Pending, …) ride as a `KonzoleTag` on an `AsyncLocal<KonzoleScopeState>` pushed via helper extensions.

**Tech Stack:** .NET 10, `Microsoft.Extensions.Logging` 10.0.0, `Microsoft.Extensions.Http` 10.0.0, `System.Threading.Channels`, `System.Net.Http` (with `IHttpClientFactory`), `System.Text.Json`, xUnit (for tests).

**Spec:** `docs/superpowers/specs/2026-05-17-konzole-overhaul-design.md`

---

## File map

### Created
- `Rymote.Konzole/Models/KonzoleTag.cs`
- `Rymote.Konzole/Models/KonzoleScopeState.cs`
- `Rymote.Konzole/Formatters/FormatterContext.cs`
- `Rymote.Konzole/Formatters/FormatterHelpers.cs`
- `Rymote.Konzole/Formatters/Json/ExceptionJsonConverter.cs`
- `Rymote.Konzole/Sinks/Files/FileRollingPolicy.cs`
- `Rymote.Konzole/Sinks/Files/IFileRollingStrategy.cs`
- `Rymote.Konzole/Sinks/Files/SizeOnlyRollingStrategy.cs`
- `Rymote.Konzole/Sinks/Files/DateOnlyRollingStrategy.cs`
- `Rymote.Konzole/Sinks/Files/DateThenSizeRollingStrategy.cs`
- `Rymote.Konzole/Sinks/Http/HttpSinkBase.cs`
- `Rymote.Konzole/Sinks/Http/HttpSinkOptionsBase.cs`
- `Rymote.Konzole/Diagnostics/KonzoleDiagnostics.cs`
- `Rymote.Konzole/Diagnostics/SinkErrorEventArgs.cs`
- `Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj`
- `Rymote.Konzole.Tests/Infrastructure/FakeClock.cs`
- `Rymote.Konzole.Tests/Infrastructure/FakeSink.cs`
- `Rymote.Konzole.Tests/Infrastructure/FakeHttpMessageHandler.cs`
- `Rymote.Konzole.Tests/Formatters/ConsoleFormatterTests.cs`
- `Rymote.Konzole.Tests/Formatters/JsonFormatterTests.cs`
- `Rymote.Konzole.Tests/Formatters/DiscordFormatterTests.cs`
- `Rymote.Konzole.Tests/Formatters/SlackFormatterTests.cs`
- `Rymote.Konzole.Tests/Dispatch/KonzoleLoggerTests.cs`
- `Rymote.Konzole.Tests/Dispatch/SinkBaseBackpressureTests.cs`
- `Rymote.Konzole.Tests/Dispatch/GracefulShutdownTests.cs`
- `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs`
- `Rymote.Konzole.Tests/Sinks/FileSinkRotationTests.cs`
- `Rymote.Konzole.Tests/Sinks/HttpSinkRetryTests.cs`
- `Rymote.Konzole.Tests/Configuration/KonzoleBuilderTests.cs`
- `Rymote.Konzole.Tests/Configuration/LoggerExtensionsTests.cs`
- `README.md`

### Modified
- `Rymote.Konzole/Rymote.Konzole.csproj` (NuGet metadata, `Microsoft.Extensions.Http`)
- `Rymote.Konzole/KonzoleLogger.cs` (rewritten)
- `Rymote.Konzole/KonzoleLoggerProvider.cs` (rewritten)
- `Rymote.Konzole/Configuration/KonzoleBuilder.cs` (rewritten)
- `Rymote.Konzole/Configuration/SinkOptionsBase.cs` (added MaxQueueSize, ShutdownTimeout, MaxMessageLength)
- `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs` (LevelColors/TagColors maps; MaxMessageLength removed)
- `Rymote.Konzole/Configuration/FileSinkOptions.cs` (RollingPolicy, FlushInterval)
- `Rymote.Konzole/Configuration/RemoteSinkOptions.cs` (extends `HttpSinkOptionsBase`)
- `Rymote.Konzole/Configuration/DiscordSinkOptions.cs` (extends `HttpSinkOptionsBase`; MaxMessageLength removed)
- `Rymote.Konzole/Configuration/SlackSinkOptions.cs` (extends `HttpSinkOptionsBase`; MaxMessageLength removed)
- `Rymote.Konzole/Models/LogEntry.cs` (record, DateTimeOffset, KonzoleTag, LogLevel)
- `Rymote.Konzole/Models/LogIcon.cs` (LogLevel + KonzoleTag overloads)
- `Rymote.Konzole/Formatters/ILogFormatter.cs` (new signature)
- `Rymote.Konzole/Formatters/ConsoleFormatter.cs` (rewritten)
- `Rymote.Konzole/Formatters/JsonFormatter.cs` (rewritten, wires `ExceptionJsonConverter`)
- `Rymote.Konzole/Formatters/DiscordFormatter.cs` (rewritten)
- `Rymote.Konzole/Formatters/SlackFormatter.cs` (rewritten)
- `Rymote.Konzole/Sinks/ISink.cs` (TryEnqueue, FlushAsync)
- `Rymote.Konzole/Sinks/SinkBase.cs` (channel + worker)
- `Rymote.Konzole/Sinks/ConsoleSink.cs` (rewritten)
- `Rymote.Konzole/Sinks/FileSink.cs` (rewritten, uses rolling strategies)
- `Rymote.Konzole/Sinks/RemoteSink.cs` (extends `HttpSinkBase`)
- `Rymote.Konzole/Sinks/DiscordSink.cs` (extends `HttpSinkBase`)
- `Rymote.Konzole/Sinks/SlackSink.cs` (extends `HttpSinkBase`)
- `Rymote.Konzole/Extensions/LoggerExtensions.cs` (scope-pushing helpers)
- `Rymote.Konzole/Extensions/LoggingBuilderExtensions.cs` (only `AddKonzole` survives)
- `Rymote.Konzole.sln` (add test project)
- `.gitignore` (logs and *.log entries)

### Deleted
- `Rymote.Konzole/Formatters/FormatterBase.cs`
- `Rymote.Konzole/Models/LogLevel.cs` (the `KonzoleLogLevel` enum)
- `Rymote.Konzole/logs/app.log`
- `Rymote.Konzole/logs/production.log`

---

## Phase 1 — Cleanup & scaffolding

### Task 1: Update `.gitignore` and remove tracked log artifacts

**Files:**
- Modify: `.gitignore`
- Delete: `Rymote.Konzole/logs/app.log`, `Rymote.Konzole/logs/production.log`

- [ ] **Step 1: Append log patterns to `.gitignore`**

Append these two lines to `.gitignore`:

```
**/logs/
*.log
```

- [ ] **Step 2: Untrack the existing log files**

```bash
git rm --cached Rymote.Konzole/logs/app.log Rymote.Konzole/logs/production.log
```

- [ ] **Step 3: Verify no log files appear in `git status`**

```bash
git status --short
```

Expected: no entries under `Rymote.Konzole/logs/`.

- [ ] **Step 4: Commit (ask user first)**

```bash
git add .gitignore
git commit -m "chore: ignore log artifacts and untrack committed log files"
```

---

### Task 2: Add NuGet metadata and `Microsoft.Extensions.Http` reference to `Rymote.Konzole.csproj`

**Files:**
- Modify: `Rymote.Konzole/Rymote.Konzole.csproj`
- Create: `README.md` (placeholder for now)

- [ ] **Step 1: Replace `Rymote.Konzole/Rymote.Konzole.csproj` content**

Overwrite the file with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

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
        <NoWarn>$(NoWarn);CS1591</NoWarn>
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

</Project>
```

`CS1591` is suppressed during the rewrite so a missing XML doc on a public type doesn't break the build. Removed in the final polish if desired.

- [ ] **Step 2: Create a placeholder `README.md` at repo root**

Create `README.md` with the single line:

```markdown
# Rymote.Konzole
```

(Phase 9 replaces it with the real content.)

- [ ] **Step 3: Restore and build**

```bash
dotnet restore
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: build succeeds. (Source files still reference the old API — they will be replaced in later phases.)

- [ ] **Step 4: Commit (ask user first)**

```bash
git add Rymote.Konzole/Rymote.Konzole.csproj README.md
git commit -m "build: add NuGet metadata, README placeholder, IHttpClientFactory dep"
```

---

### Task 3: Scaffold `Rymote.Konzole.Tests` xUnit project

**Files:**
- Create: `Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj`
- Modify: `Rymote.Konzole.sln`

- [ ] **Step 1: Create the test project**

```bash
dotnet new xunit -n Rymote.Konzole.Tests -o Rymote.Konzole.Tests --framework net10.0
```

- [ ] **Step 2: Reference the main project**

```bash
dotnet add Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj reference Rymote.Konzole/Rymote.Konzole.csproj
```

- [ ] **Step 3: Add `Microsoft.Extensions.Http` to the test project**

```bash
dotnet add Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj package Microsoft.Extensions.Http --version 10.0.0
```

- [ ] **Step 4: Add the test project to the solution**

```bash
dotnet sln Rymote.Konzole.sln add Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj
```

- [ ] **Step 5: Run the default xUnit placeholder test to confirm runner works**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj
```

Expected: 1 passing test (auto-generated).

- [ ] **Step 6: Delete the auto-generated placeholder**

```bash
rm Rymote.Konzole.Tests/UnitTest1.cs
```

- [ ] **Step 7: Commit (ask user first)**

```bash
git add Rymote.Konzole.sln Rymote.Konzole.Tests/
git commit -m "test: scaffold Rymote.Konzole.Tests xUnit project"
```

---

### Task 4: Add `FakeClock` test helper

**Files:**
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeClock.cs`
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeClockTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Rymote.Konzole.Tests/Infrastructure/FakeClockTests.cs`:

```csharp
using Xunit;

namespace Rymote.Konzole.Tests.Infrastructure;

public class FakeClockTests
{
    [Fact]
    public void Now_ReturnsInitialTime_UntilAdvanced()
    {
        DateTimeOffset initialTime = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
        FakeClock clock = new(initialTime);

        Assert.Equal(initialTime, clock.Now());

        clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(initialTime.AddHours(2), clock.Now());
    }
}
```

- [ ] **Step 2: Run the test (expect compile failure)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~FakeClockTests
```

Expected: build error referencing `FakeClock`.

- [ ] **Step 3: Create `FakeClock`**

Create `Rymote.Konzole.Tests/Infrastructure/FakeClock.cs`:

```csharp
namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeClock
{
    private DateTimeOffset _currentTime;

    public FakeClock(DateTimeOffset initialTime)
    {
        _currentTime = initialTime;
    }

    public DateTimeOffset Now() => _currentTime;

    public void Advance(TimeSpan duration) => _currentTime = _currentTime.Add(duration);

    public void SetTo(DateTimeOffset moment) => _currentTime = moment;
}
```

- [ ] **Step 4: Run the test (expect pass)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~FakeClockTests
```

Expected: 1 passing test.

- [ ] **Step 5: Commit (ask user first)**

```bash
git add Rymote.Konzole.Tests/Infrastructure/
git commit -m "test: add FakeClock helper"
```

---

### Task 5: Add `FakeHttpMessageHandler` test helper

**Files:**
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeHttpMessageHandler.cs`
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeHttpMessageHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Rymote.Konzole.Tests/Infrastructure/FakeHttpMessageHandlerTests.cs`:

```csharp
using System.Net;
using System.Net.Http;
using Xunit;

namespace Rymote.Konzole.Tests.Infrastructure;

public class FakeHttpMessageHandlerTests
{
    [Fact]
    public async Task QueuedResponses_AreReturnedInOrder_AndRequestsRecorded()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        using HttpClient httpClient = new(handler);

        HttpResponseMessage firstResponse = await httpClient.GetAsync("https://example.test/first");
        HttpResponseMessage secondResponse = await httpClient.GetAsync("https://example.test/second");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        Assert.Equal(2, handler.RecordedRequests.Count);
        Assert.Equal("https://example.test/first", handler.RecordedRequests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task EnqueuedException_IsThrownOnNextSend()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueException(new HttpRequestException("network down"));

        using HttpClient httpClient = new(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => httpClient.GetAsync("https://example.test/x"));
    }
}
```

- [ ] **Step 2: Run the tests (expect compile failure)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~FakeHttpMessageHandlerTests
```

Expected: build error referencing `FakeHttpMessageHandler`.

- [ ] **Step 3: Create `FakeHttpMessageHandler`**

Create `Rymote.Konzole.Tests/Infrastructure/FakeHttpMessageHandler.cs`:

```csharp
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<Func<HttpRequestMessage, HttpResponseMessage>> _responseFactories = new();
    private readonly ConcurrentQueue<HttpRequestMessage> _recordedRequests = new();

    public IReadOnlyList<HttpRequestMessage> RecordedRequests => _recordedRequests.ToArray();

    public void EnqueueResponse(HttpResponseMessage response) =>
        _responseFactories.Enqueue(_ => response);

    public void EnqueueResponse(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
        _responseFactories.Enqueue(responseFactory);

    public void EnqueueException(Exception exception) =>
        _responseFactories.Enqueue(_ => throw exception);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _recordedRequests.Enqueue(request);

        if (!_responseFactories.TryDequeue(out Func<HttpRequestMessage, HttpResponseMessage>? factory))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        return Task.FromResult(factory(request));
    }
}
```

- [ ] **Step 4: Run the tests (expect pass)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~FakeHttpMessageHandlerTests
```

Expected: 2 passing tests.

- [ ] **Step 5: Commit (ask user first)**

```bash
git add Rymote.Konzole.Tests/Infrastructure/
git commit -m "test: add FakeHttpMessageHandler"
```

> **Note about commit cadence from here onward:** Phases 3-7 rewrite interdependent types — `LogEntry`, `ILogFormatter`, `ISink`, `SinkBase`, the concrete sinks, and `KonzoleLogger`. The working tree will be temporarily red between tasks within a phase. Each phase ends with a green build. Treat tasks within a phase as a group: stage everything, run `dotnet build` + `dotnet test` at the end of the phase, then commit. If you prefer smaller commits, the in-phase tasks can each be committed individually with a `wip:` prefix and squashed at phase end.

---

## Phase 2 — Additive model types

These tasks add new types alongside the existing `KonzoleLogLevel`. The working tree stays green throughout.

### Task 6: Add `KonzoleTag` enum

**Files:**
- Create: `Rymote.Konzole/Models/KonzoleTag.cs`

- [ ] **Step 1: Create the enum**

```csharp
namespace Rymote.Konzole.Models;

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

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: green.

---

### Task 7: Add `KonzoleScopeState` with `AsyncLocal` current

**Files:**
- Create: `Rymote.Konzole/Models/KonzoleScopeState.cs`
- Create: `Rymote.Konzole.Tests/Models/KonzoleScopeStateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Models;

public class KonzoleScopeStateTests
{
    [Fact]
    public void Push_SetsCurrent_DisposeRestoresPrevious()
    {
        Assert.Null(KonzoleScopeState.Current);

        KonzoleScopeState outerState = new() { Tag = KonzoleTag.Start };
        using (KonzoleScopeState.Push(outerState))
        {
            Assert.Same(outerState, KonzoleScopeState.Current);

            KonzoleScopeState innerState = new() { Tag = KonzoleTag.Success };
            using (KonzoleScopeState.Push(innerState))
            {
                Assert.Same(innerState, KonzoleScopeState.Current);
            }

            Assert.Same(outerState, KonzoleScopeState.Current);
        }

        Assert.Null(KonzoleScopeState.Current);
    }

    [Fact]
    public async Task Current_SurvivesAwait()
    {
        KonzoleScopeState pushedState = new() { Tag = KonzoleTag.Watch };
        using (KonzoleScopeState.Push(pushedState))
        {
            await Task.Yield();
            Assert.Same(pushedState, KonzoleScopeState.Current);
        }
    }
}
```

- [ ] **Step 2: Run (expect compile failure)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~KonzoleScopeStateTests
```

- [ ] **Step 3: Create `KonzoleScopeState`**

```csharp
namespace Rymote.Konzole.Models;

public sealed class KonzoleScopeState
{
    private static readonly AsyncLocal<KonzoleScopeState?> CurrentScope = new();

    public KonzoleTag? Tag { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }

    public static KonzoleScopeState? Current => CurrentScope.Value;

    public static IDisposable Push(KonzoleScopeState scopeState)
    {
        KonzoleScopeState? previousScope = CurrentScope.Value;
        CurrentScope.Value = scopeState;
        return new PopOnDispose(previousScope);
    }

    private sealed class PopOnDispose : IDisposable
    {
        private readonly KonzoleScopeState? _previousScope;
        private bool _alreadyDisposed;

        public PopOnDispose(KonzoleScopeState? previousScope)
        {
            _previousScope = previousScope;
        }

        public void Dispose()
        {
            if (_alreadyDisposed) return;
            _alreadyDisposed = true;
            CurrentScope.Value = _previousScope;
        }
    }
}
```

> **One-OOP-per-file exception:** `PopOnDispose` is a `private sealed` helper used only by `KonzoleScopeState.Push`. Splitting it into its own file would require making it `internal` and would not improve readability. Keep it nested.

- [ ] **Step 4: Run (expect pass)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~KonzoleScopeStateTests
```

Expected: 2 passing tests.

---

### Task 8: Add `LogLevel` and `KonzoleTag` overloads to `LogIcon`

**Files:**
- Modify: `Rymote.Konzole/Models/LogIcon.cs`

- [ ] **Step 1: Replace `LogIcon.cs` contents**

```csharp
using Microsoft.Extensions.Logging;

namespace Rymote.Konzole.Models;

public static class LogIcon
{
    public static string GetIcon(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace       => "🔍",
        LogLevel.Debug       => "🐛",
        LogLevel.Information => "ℹ️",
        LogLevel.Warning     => "⚠️",
        LogLevel.Error       => "❌",
        LogLevel.Critical    => "💀",
        _                    => "•"
    };

    public static string GetIcon(KonzoleTag tag) => tag switch
    {
        KonzoleTag.Success  => "✅",
        KonzoleTag.Pending  => "⏳",
        KonzoleTag.Complete => "✔️",
        KonzoleTag.Note     => "📝",
        KonzoleTag.Start    => "🚀",
        KonzoleTag.Pause    => "⏸️",
        KonzoleTag.Watch    => "👁️",
        _                   => "•"
    };

    public static string GetFallbackIcon(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace       => "[TRACE]",
        LogLevel.Debug       => "[DEBUG]",
        LogLevel.Information => "[INFO]",
        LogLevel.Warning     => "[WARN]",
        LogLevel.Error       => "[ERROR]",
        LogLevel.Critical    => "[FATAL]",
        _                    => "[LOG]"
    };

    public static string GetFallbackIcon(KonzoleTag tag) => tag switch
    {
        KonzoleTag.Success  => "[SUCCESS]",
        KonzoleTag.Pending  => "[PENDING]",
        KonzoleTag.Complete => "[DONE]",
        KonzoleTag.Note     => "[NOTE]",
        KonzoleTag.Start    => "[START]",
        KonzoleTag.Pause    => "[PAUSE]",
        KonzoleTag.Watch    => "[WATCH]",
        _                   => "[LOG]"
    };
}
```

The original `KonzoleLogLevel` overloads are removed in Phase 3 (along with the enum itself). For now this file no longer has `KonzoleLogLevel` overloads — that breaks any caller using `LogIcon.GetIcon(KonzoleLogLevel)`. The only such caller is `ConsoleFormatter.cs` and `DiscordFormatter.cs` / `SlackFormatter.cs`, all rewritten in Phase 3. To keep this phase building green, **temporarily** add a fourth method that delegates:

```csharp
[Obsolete("Use GetIcon(LogLevel) or GetIcon(KonzoleTag); removed in Phase 3.")]
public static string GetIcon(KonzoleLogLevel level) => level switch
{
    KonzoleLogLevel.Trace       => GetIcon(LogLevel.Trace),
    KonzoleLogLevel.Debug       => GetIcon(LogLevel.Debug),
    KonzoleLogLevel.Information => GetIcon(LogLevel.Information),
    KonzoleLogLevel.Warning     => GetIcon(LogLevel.Warning),
    KonzoleLogLevel.Error       => GetIcon(LogLevel.Error),
    KonzoleLogLevel.Fatal       => GetIcon(LogLevel.Critical),
    KonzoleLogLevel.Success     => GetIcon(KonzoleTag.Success),
    KonzoleLogLevel.Pending     => GetIcon(KonzoleTag.Pending),
    KonzoleLogLevel.Complete    => GetIcon(KonzoleTag.Complete),
    KonzoleLogLevel.Note        => GetIcon(KonzoleTag.Note),
    KonzoleLogLevel.Start       => GetIcon(KonzoleTag.Start),
    KonzoleLogLevel.Pause       => GetIcon(KonzoleTag.Pause),
    KonzoleLogLevel.Watch       => GetIcon(KonzoleTag.Watch),
    _                            => "•"
};

[Obsolete("Use GetFallbackIcon(LogLevel) or GetFallbackIcon(KonzoleTag); removed in Phase 3.")]
public static string GetFallbackIcon(KonzoleLogLevel level) => level switch
{
    KonzoleLogLevel.Trace       => GetFallbackIcon(LogLevel.Trace),
    KonzoleLogLevel.Debug       => GetFallbackIcon(LogLevel.Debug),
    KonzoleLogLevel.Information => GetFallbackIcon(LogLevel.Information),
    KonzoleLogLevel.Warning     => GetFallbackIcon(LogLevel.Warning),
    KonzoleLogLevel.Error       => GetFallbackIcon(LogLevel.Error),
    KonzoleLogLevel.Fatal       => GetFallbackIcon(LogLevel.Critical),
    KonzoleLogLevel.Success     => GetFallbackIcon(KonzoleTag.Success),
    KonzoleLogLevel.Pending     => GetFallbackIcon(KonzoleTag.Pending),
    KonzoleLogLevel.Complete    => GetFallbackIcon(KonzoleTag.Complete),
    KonzoleLogLevel.Note        => GetFallbackIcon(KonzoleTag.Note),
    KonzoleLogLevel.Start       => GetFallbackIcon(KonzoleTag.Start),
    KonzoleLogLevel.Pause       => GetFallbackIcon(KonzoleTag.Pause),
    KonzoleLogLevel.Watch       => GetFallbackIcon(KonzoleTag.Watch),
    _                            => "[LOG]"
};
```

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: green (with `[Obsolete]` warnings on the legacy overloads — that's fine).

- [ ] **Step 3: Commit Phase 2 (ask user first)**

```bash
git add Rymote.Konzole/Models/ Rymote.Konzole.Tests/Models/
git commit -m "feat: add KonzoleTag, KonzoleScopeState, LogLevel/Tag LogIcon overloads"
```

---

## Phase 3 — Formatters & LogEntry redesign

**This phase leaves the working tree RED.** Sinks and the logger reference the old `KonzoleLogLevel` and the old `LogEntry` shape — they are rebuilt in Phases 4-8. Commit at the end of Phase 8.

### Task 9: Replace `ILogFormatter` interface + add `FormatterContext` + `FormatterHelpers`

**Files:**
- Modify: `Rymote.Konzole/Formatters/ILogFormatter.cs`
- Create: `Rymote.Konzole/Formatters/FormatterContext.cs`
- Create: `Rymote.Konzole/Formatters/FormatterHelpers.cs`
- Delete: `Rymote.Konzole/Formatters/FormatterBase.cs`

- [ ] **Step 1: Create `FormatterContext.cs`**

```csharp
namespace Rymote.Konzole.Formatters;

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
```

- [ ] **Step 2: Replace `ILogFormatter.cs`**

```csharp
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public interface ILogFormatter
{
    string Format(LogEntry entry, FormatterContext context);
}
```

- [ ] **Step 3: Create `FormatterHelpers.cs`**

```csharp
using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

internal static class FormatterHelpers
{
    private const int CategoryMaxLength = 30;

    public static void AppendTimestamp(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowTimestamp) return;
        stringBuilder.Append('[');
        stringBuilder.Append(entry.Timestamp.ToString(context.TimestampFormat));
        stringBuilder.Append("] ");
    }

    public static void AppendCategory(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowCategory || string.IsNullOrEmpty(entry.Category)) return;
        stringBuilder.Append('[');
        stringBuilder.Append(TruncateCategory(entry.Category));
        stringBuilder.Append("] ");
    }

    public static void AppendEventId(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowEventId || entry.EventId.Id == 0) return;
        stringBuilder.Append('[');
        stringBuilder.Append(entry.EventId.Id);
        if (!string.IsNullOrEmpty(entry.EventId.Name))
        {
            stringBuilder.Append(':');
            stringBuilder.Append(entry.EventId.Name);
        }
        stringBuilder.Append("] ");
    }

    public static void AppendScope(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowScope || string.IsNullOrEmpty(entry.Scope)) return;
        stringBuilder.Append("=> ");
        stringBuilder.Append(entry.Scope);
        stringBuilder.Append(' ');
    }

    public static void AppendException(StringBuilder stringBuilder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowException || entry.Exception == null) return;
        stringBuilder.AppendLine();
        stringBuilder.Append("    Exception: ");
        stringBuilder.AppendLine(entry.Exception.GetType().Name);
        stringBuilder.Append("    Message: ");
        stringBuilder.AppendLine(entry.Exception.Message);
        if (string.IsNullOrEmpty(entry.Exception.StackTrace)) return;
        stringBuilder.AppendLine("    Stack Trace:");
        foreach (string line in entry.Exception.StackTrace.Split('\n'))
        {
            stringBuilder.Append("      ");
            stringBuilder.AppendLine(line.Trim());
        }
    }

    public static string TruncateMessage(string message, int maxLength) =>
        message.Length <= maxLength ? message : message.Substring(0, maxLength - 3) + "...";

    private static string TruncateCategory(string category)
    {
        if (category.Length <= CategoryMaxLength) return category;

        string[] parts = category.Split('.');
        if (parts.Length == 1) return category.Substring(0, CategoryMaxLength - 3) + "...";

        StringBuilder builder = new();
        for (int index = 0; index < parts.Length - 1; index++)
        {
            builder.Append(parts[index][0]);
            builder.Append('.');
        }
        builder.Append(parts[^1]);

        return builder.Length > CategoryMaxLength
            ? builder.ToString(0, CategoryMaxLength - 3) + "..."
            : builder.ToString();
    }
}
```

- [ ] **Step 4: Delete `FormatterBase.cs`**

```bash
rm Rymote.Konzole/Formatters/FormatterBase.cs
```

(The build is now red — concrete formatters still reference `FormatterBase`. Continue.)

---

### Task 10: Replace `LogEntry` with new record; delete `KonzoleLogLevel`

**Files:**
- Modify: `Rymote.Konzole/Models/LogEntry.cs`
- Modify: `Rymote.Konzole/Models/LogIcon.cs` (remove `[Obsolete]` overloads)
- Delete: `Rymote.Konzole/Models/LogLevel.cs`

- [ ] **Step 1: Replace `LogEntry.cs`**

```csharp
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Rymote.Konzole.Models;

public sealed record LogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public LogLevel Level { get; init; }
    public KonzoleTag? Tag { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Category { get; init; }
    public EventId EventId { get; init; }

    [JsonIgnore]
    public Exception? Exception { get; init; }

    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
    public string? Scope { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
```

- [ ] **Step 2: Delete `Models/LogLevel.cs`**

```bash
rm Rymote.Konzole/Models/LogLevel.cs
```

- [ ] **Step 3: Remove `[Obsolete] KonzoleLogLevel` overloads from `LogIcon.cs`**

Edit `Rymote.Konzole/Models/LogIcon.cs` and delete both `[Obsolete]` overloads added in Task 8. The remaining file contains only `GetIcon(LogLevel)`, `GetIcon(KonzoleTag)`, `GetFallbackIcon(LogLevel)`, `GetFallbackIcon(KonzoleTag)`.

---

### Task 11: Rewrite `ConsoleFormatter` (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/ConsoleFormatter.cs`
- Create: `Rymote.Konzole.Tests/Formatters/ConsoleFormatterTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class ConsoleFormatterTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_UsesLevelFallbackIcon_WhenNoTag()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "hello" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[INFO]", rendered);
        Assert.Contains("hello", rendered);
    }

    [Fact]
    public void Format_UsesTagFallbackIcon_WhenTagSet()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "ok" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[SUCCESS]", rendered);
        Assert.DoesNotContain("[INFO]", rendered);
    }

    [Fact]
    public void Format_TruncatesMessage_AtMaxMessageLength()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { MaxMessageLength = 10, ShowTimestamp = false, ShowCategory = false };
        LogEntry entry = new() { Level = LogLevel.Information, Message = "abcdefghijklmnop" };

        string rendered = formatter.Format(entry, context);

        Assert.EndsWith("abcdefg...", rendered);
    }

    [Fact]
    public void Format_RendersProperties_InParens()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        Dictionary<string, object?> properties = new() { ["userId"] = 42, ["action"] = "login" };
        LogEntry entry = new()
        {
            Level = LogLevel.Information,
            Message = "event",
            Properties = properties
        };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("(userId: 42, action: login)", rendered);
    }
}
```

- [ ] **Step 2: Replace `ConsoleFormatter.cs`**

```csharp
using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class ConsoleFormatter : ILogFormatter
{
    private readonly bool _useEmojis;

    public ConsoleFormatter(bool useEmojis = true)
    {
        _useEmojis = useEmojis;
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        bool consoleSupportsUtf8 = Console.OutputEncoding.CodePage == 65001;
        bool renderEmoji = _useEmojis && consoleSupportsUtf8;

        if (renderEmoji)
        {
            string emoji = entry.Tag.HasValue
                ? LogIcon.GetIcon(entry.Tag.Value)
                : LogIcon.GetIcon(entry.Level);
            stringBuilder.Append(emoji);
            stringBuilder.Append("  ");
        }
        else
        {
            string fallback = entry.Tag.HasValue
                ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                : LogIcon.GetFallbackIcon(entry.Level);
            stringBuilder.Append(fallback);
            stringBuilder.Append(' ');
        }

        FormatterHelpers.AppendTimestamp(stringBuilder, entry, context);
        FormatterHelpers.AppendCategory(stringBuilder, entry, context);
        FormatterHelpers.AppendEventId(stringBuilder, entry, context);
        FormatterHelpers.AppendScope(stringBuilder, entry, context);

        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));

        if (entry.Properties is { Count: > 0 })
        {
            stringBuilder.Append(" (");
            bool isFirst = true;
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                if (!isFirst) stringBuilder.Append(", ");
                stringBuilder.Append(property.Key);
                stringBuilder.Append(": ");
                stringBuilder.Append(property.Value?.ToString() ?? "null");
                isFirst = false;
            }
            stringBuilder.Append(')');
        }

        FormatterHelpers.AppendException(stringBuilder, entry, context);

        return stringBuilder.ToString();
    }
}
```

> **Note:** The formatter no longer takes `ConsoleSinkOptions`. The constructor takes only the `useEmojis` flag because it interacts with `Console.OutputEncoding` for the fallback decision. All other knobs flow through `FormatterContext`.

- [ ] **Step 3: Run the tests once the rest of Phase 3 compiles**

The build of the test project will fail until the other formatters and the sinks compile. The test commands are run at the end of Phase 8 (full green checkpoint).

---

### Task 12: Rewrite `JsonFormatter` and extract `ExceptionJsonConverter`

**Files:**
- Modify: `Rymote.Konzole/Formatters/JsonFormatter.cs`
- Create: `Rymote.Konzole/Formatters/Json/ExceptionJsonConverter.cs`
- Create: `Rymote.Konzole.Tests/Formatters/JsonFormatterTests.cs`

- [ ] **Step 1: Create `ExceptionJsonConverter.cs`**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rymote.Konzole.Formatters.Json;

public sealed class ExceptionJsonConverter : JsonConverter<Exception>
{
    public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("ExceptionJsonConverter is write-only.");

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.GetType().FullName);
        writer.WriteString("message", value.Message);
        writer.WriteString("stackTrace", value.StackTrace);

        if (value.InnerException != null)
        {
            writer.WritePropertyName("innerException");
            Write(writer, value.InnerException, options);
        }

        writer.WriteEndObject();
    }
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class JsonFormatterTests
{
    private static readonly FormatterContext DefaultContext = new()
    {
        ShowTimestamp = false,
        ShowException = true,
        ShowCategory = true,
        ShowEventId = true,
        ShowScope = true
    };

    [Fact]
    public void Format_EmitsLevelAndMessage()
    {
        JsonFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "careful" };

        string json = formatter.Format(entry, DefaultContext);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("Warning", document.RootElement.GetProperty("level").GetString());
        Assert.Equal("careful", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void Format_NestedExceptions_UseConverter_AndIncludeInner()
    {
        JsonFormatter formatter = new();
        InvalidOperationException inner = new("inner-cause");
        ApplicationException outer = new("outer-failure", inner);
        LogEntry entry = new() { Level = LogLevel.Error, Message = "fail", Exception = outer };

        string json = formatter.Format(entry, DefaultContext);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement exceptionElement = document.RootElement.GetProperty("exception");
        Assert.Equal("outer-failure", exceptionElement.GetProperty("message").GetString());
        Assert.Equal("inner-cause", exceptionElement.GetProperty("innerException").GetProperty("message").GetString());
    }
}
```

- [ ] **Step 3: Replace `JsonFormatter.cs`**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Rymote.Konzole.Formatters.Json;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class JsonFormatter : ILogFormatter
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonFormatter()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new ExceptionJsonConverter()
            }
        };
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        Dictionary<string, object?> document = new()
        {
            ["timestamp"] = context.ShowTimestamp ? entry.Timestamp.ToString(context.TimestampFormat) : null,
            ["level"] = entry.Level.ToString(),
            ["tag"] = entry.Tag?.ToString(),
            ["message"] = entry.Message,
            ["category"] = context.ShowCategory ? entry.Category : null,
            ["eventId"] = context.ShowEventId && entry.EventId.Id != 0 ? entry.EventId.Id : (int?)null,
            ["eventName"] = context.ShowEventId ? entry.EventId.Name : null,
            ["exception"] = context.ShowException ? entry.Exception : null,
            ["properties"] = entry.Properties,
            ["scope"] = context.ShowScope ? entry.Scope : null,
            ["traceId"] = entry.TraceId,
            ["spanId"] = entry.SpanId
        };

        return JsonSerializer.Serialize(document, _jsonSerializerOptions);
    }
}
```

The constructor takes no parameters — defaults are baked in. Override by injecting a custom formatter through `SinkOptionsBase.Formatter` if needed.

---

### Task 13: Rewrite `DiscordFormatter`

**Files:**
- Modify: `Rymote.Konzole/Formatters/DiscordFormatter.cs`
- Create: `Rymote.Konzole.Tests/Formatters/DiscordFormatterTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class DiscordFormatterTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_RendersLevelInBold_AndMessageOnNewLine()
    {
        DiscordFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "watch out" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("**Warning**", rendered);
        Assert.Contains("watch out", rendered);
    }

    [Fact]
    public void Format_UsesTagIcon_WhenTagSet()
    {
        DiscordFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "done" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.StartsWith("✅", rendered);
    }
}
```

- [ ] **Step 2: Replace `DiscordFormatter.cs`**

```csharp
using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class DiscordFormatter : ILogFormatter
{
    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        string icon = entry.Tag.HasValue
            ? LogIcon.GetIcon(entry.Tag.Value)
            : LogIcon.GetIcon(entry.Level);
        stringBuilder.Append(icon);

        stringBuilder.Append(" **");
        stringBuilder.Append(entry.Tag?.ToString() ?? entry.Level.ToString());
        stringBuilder.Append("**");

        if (context.ShowTimestamp)
        {
            stringBuilder.Append(" `");
            stringBuilder.Append(entry.Timestamp.ToString(context.TimestampFormat));
            stringBuilder.Append('`');
        }

        if (context.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            stringBuilder.Append(" [");
            stringBuilder.Append(entry.Category);
            stringBuilder.Append(']');
        }

        stringBuilder.AppendLine();
        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));

        return stringBuilder.ToString();
    }
}
```

---

### Task 14: Rewrite `SlackFormatter`

**Files:**
- Modify: `Rymote.Konzole/Formatters/SlackFormatter.cs`
- Create: `Rymote.Konzole.Tests/Formatters/SlackFormatterTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class SlackFormatterTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_UsesSingleAsteriskBold_AndIcon()
    {
        SlackFormatter formatter = new();
        LogEntry entry = new() { Level = LogLevel.Error, Message = "boom" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("*Error*", rendered);
        Assert.Contains("boom", rendered);
    }
}
```

- [ ] **Step 2: Replace `SlackFormatter.cs`**

```csharp
using System.Text;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class SlackFormatter : ILogFormatter
{
    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        string icon = entry.Tag.HasValue
            ? LogIcon.GetIcon(entry.Tag.Value)
            : LogIcon.GetIcon(entry.Level);
        stringBuilder.Append(icon);

        stringBuilder.Append(" *");
        stringBuilder.Append(entry.Tag?.ToString() ?? entry.Level.ToString());
        stringBuilder.Append('*');

        if (context.ShowTimestamp)
        {
            stringBuilder.Append(" `");
            stringBuilder.Append(entry.Timestamp.ToString(context.TimestampFormat));
            stringBuilder.Append('`');
        }

        if (context.ShowCategory && !string.IsNullOrEmpty(entry.Category))
        {
            stringBuilder.Append(" [");
            stringBuilder.Append(entry.Category);
            stringBuilder.Append(']');
        }

        stringBuilder.Append('\n');
        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));

        return stringBuilder.ToString();
    }
}
```

> **End of Phase 3.** The Konzole project will not compile yet — sinks reference the deleted `KonzoleLogLevel` and old `FormatterBase`. Continue to Phase 4. Do not commit at this point.

---

## Phase 4 — Sink contracts, options, diagnostics, `SinkBase`

Still RED at end of phase. Sinks themselves are rewritten in Phases 5-7.

### Task 15: Rewrite `SinkOptionsBase`

**Files:**
- Modify: `Rymote.Konzole/Configuration/SinkOptionsBase.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;

namespace Rymote.Konzole.Configuration;

public abstract class SinkOptionsBase
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public bool ShowTimestamp { get; set; } = true;
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool ShowCategory { get; set; } = true;
    public bool ShowEventId { get; set; }
    public bool ShowScope { get; set; } = true;
    public bool ShowException { get; set; } = true;
    public int MaxMessageLength { get; set; } = 4000;

    public int MaxQueueSize { get; set; } = 10_000;
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public ILogFormatter? Formatter { get; set; }

    public FormatterContext BuildFormatterContext() => new()
    {
        ShowTimestamp = ShowTimestamp,
        TimestampFormat = TimestampFormat,
        ShowCategory = ShowCategory,
        ShowEventId = ShowEventId,
        ShowScope = ShowScope,
        ShowException = ShowException,
        MaxMessageLength = MaxMessageLength
    };
}
```

---

### Task 16: Create `HttpSinkOptionsBase`

**Files:**
- Create: `Rymote.Konzole/Sinks/Http/HttpSinkOptionsBase.cs`

- [ ] **Step 1: Create the class**

```csharp
using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Sinks.Http;

public abstract class HttpSinkOptionsBase : SinkOptionsBase
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int BatchSize { get; set; } = 100;
    public string HttpClientName { get; set; } = "Konzole.Default";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

---

### Task 17: Rewrite `ConsoleSinkOptions`

**Files:**
- Modify: `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Configuration;

public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    public bool UseEmojis { get; set; } = true;

    public IReadOnlyDictionary<LogLevel, ConsoleColor> LevelColors { get; init; } = new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Trace]       = ConsoleColor.DarkGray,
        [LogLevel.Debug]       = ConsoleColor.Gray,
        [LogLevel.Information] = ConsoleColor.Cyan,
        [LogLevel.Warning]     = ConsoleColor.Yellow,
        [LogLevel.Error]       = ConsoleColor.Red,
        [LogLevel.Critical]    = ConsoleColor.White
    };

    public IReadOnlyDictionary<KonzoleTag, ConsoleColor> TagColors { get; init; } = new Dictionary<KonzoleTag, ConsoleColor>
    {
        [KonzoleTag.Success]  = ConsoleColor.Green,
        [KonzoleTag.Pending]  = ConsoleColor.Blue,
        [KonzoleTag.Complete] = ConsoleColor.DarkGreen,
        [KonzoleTag.Note]     = ConsoleColor.Magenta,
        [KonzoleTag.Start]    = ConsoleColor.DarkCyan,
        [KonzoleTag.Pause]    = ConsoleColor.DarkYellow,
        [KonzoleTag.Watch]    = ConsoleColor.DarkMagenta
    };

    public ConsoleColor CriticalBackgroundColor { get; init; } = ConsoleColor.DarkRed;
}
```

---

### Task 18: Rewrite `FileSinkOptions`

**Files:**
- Modify: `Rymote.Konzole/Configuration/FileSinkOptions.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Rymote.Konzole.Sinks.Files;

namespace Rymote.Konzole.Configuration;

public sealed class FileSinkOptions : SinkOptionsBase
{
    public string? FilePath { get; set; }
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    public int MaxFiles { get; set; } = 5;
    public FileRollingPolicy RollingPolicy { get; set; } = FileRollingPolicy.SizeOnly;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.Zero;
}
```

(`FileRollingPolicy` is created in Phase 5 — referenced here forward.)

---

### Task 19: Convert HTTP-sink options classes to extend `HttpSinkOptionsBase`

**Files:**
- Modify: `Rymote.Konzole/Configuration/RemoteSinkOptions.cs`
- Modify: `Rymote.Konzole/Configuration/DiscordSinkOptions.cs`
- Modify: `Rymote.Konzole/Configuration/SlackSinkOptions.cs`

- [ ] **Step 1: Replace `RemoteSinkOptions.cs`**

```csharp
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class RemoteSinkOptions : HttpSinkOptionsBase
{
    public string? RemoteEndpoint { get; set; }
    public string? RemoteApiKey { get; set; }

    public RemoteSinkOptions()
    {
        HttpClientName = "Konzole.Remote";
    }
}
```

- [ ] **Step 2: Replace `DiscordSinkOptions.cs`**

```csharp
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class DiscordSinkOptions : HttpSinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    public string? Username { get; set; } = "Konzole Logger";
    public string? AvatarUrl { get; set; }
    public bool UseEmbeds { get; set; } = true;

    public IReadOnlyDictionary<KonzoleTag, int> TagEmbedColors { get; init; } = new Dictionary<KonzoleTag, int>
    {
        [KonzoleTag.Success]  = 0x00FF00,
        [KonzoleTag.Pending]  = 0x0000FF,
        [KonzoleTag.Complete] = 0x008000,
        [KonzoleTag.Note]     = 0xFF00FF,
        [KonzoleTag.Start]    = 0x00CED1,
        [KonzoleTag.Pause]    = 0xFFD700,
        [KonzoleTag.Watch]    = 0x8B008B
    };

    public IReadOnlyDictionary<Microsoft.Extensions.Logging.LogLevel, int> LevelEmbedColors { get; init; }
        = new Dictionary<Microsoft.Extensions.Logging.LogLevel, int>
    {
        [Microsoft.Extensions.Logging.LogLevel.Trace]       = 0x808080,
        [Microsoft.Extensions.Logging.LogLevel.Debug]       = 0x9B9B9B,
        [Microsoft.Extensions.Logging.LogLevel.Information] = 0x00D4FF,
        [Microsoft.Extensions.Logging.LogLevel.Warning]     = 0xFFFF00,
        [Microsoft.Extensions.Logging.LogLevel.Error]       = 0xFF0000,
        [Microsoft.Extensions.Logging.LogLevel.Critical]    = 0x8B0000
    };

    public DiscordSinkOptions()
    {
        HttpClientName = "Konzole.Discord";
        BatchSize = 1;
        MaxMessageLength = 2000;
    }
}
```

- [ ] **Step 3: Replace `SlackSinkOptions.cs`**

```csharp
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Configuration;

public sealed class SlackSinkOptions : HttpSinkOptionsBase
{
    public string? WebhookUrl { get; set; }
    public string? Channel { get; set; }
    public string? Username { get; set; } = "Konzole Logger";
    public string? IconEmoji { get; set; } = ":robot_face:";
    public string? IconUrl { get; set; }
    public bool UseAttachments { get; set; } = true;

    public IReadOnlyDictionary<KonzoleTag, string> TagAttachmentColors { get; init; } = new Dictionary<KonzoleTag, string>
    {
        [KonzoleTag.Success]  = "#00FF00",
        [KonzoleTag.Pending]  = "#0000FF",
        [KonzoleTag.Complete] = "#008000",
        [KonzoleTag.Note]     = "#FF00FF",
        [KonzoleTag.Start]    = "#00CED1",
        [KonzoleTag.Pause]    = "#FFD700",
        [KonzoleTag.Watch]    = "#8B008B"
    };

    public IReadOnlyDictionary<Microsoft.Extensions.Logging.LogLevel, string> LevelAttachmentColors { get; init; }
        = new Dictionary<Microsoft.Extensions.Logging.LogLevel, string>
    {
        [Microsoft.Extensions.Logging.LogLevel.Trace]       = "#808080",
        [Microsoft.Extensions.Logging.LogLevel.Debug]       = "#9B9B9B",
        [Microsoft.Extensions.Logging.LogLevel.Information] = "#00D4FF",
        [Microsoft.Extensions.Logging.LogLevel.Warning]     = "#FFFF00",
        [Microsoft.Extensions.Logging.LogLevel.Error]       = "#FF0000",
        [Microsoft.Extensions.Logging.LogLevel.Critical]    = "#8B0000"
    };

    public SlackSinkOptions()
    {
        HttpClientName = "Konzole.Slack";
        BatchSize = 1;
        MaxMessageLength = 3000;
    }
}
```

---

### Task 20: Rewrite `ISink` interface

**Files:**
- Modify: `Rymote.Konzole/Sinks/ISink.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public interface ISink : IAsyncDisposable, IDisposable
{
    string Name { get; }
    LogLevel MinimumLevel { get; }

    void TryEnqueue(LogEntry entry);
    ValueTask FlushAsync(CancellationToken cancellationToken);
}
```

---

### Task 21: Create `KonzoleDiagnostics` + `SinkErrorEventArgs`

**Files:**
- Create: `Rymote.Konzole/Diagnostics/SinkErrorEventArgs.cs`
- Create: `Rymote.Konzole/Diagnostics/KonzoleDiagnostics.cs`

- [ ] **Step 1: Create `SinkErrorEventArgs.cs`**

```csharp
namespace Rymote.Konzole.Diagnostics;

public sealed class SinkErrorEventArgs : EventArgs
{
    public string SinkName { get; }
    public Exception? Exception { get; }
    public string Message { get; }
    public int DroppedEntries { get; }

    public SinkErrorEventArgs(string sinkName, string message, Exception? exception = null, int droppedEntries = 0)
    {
        SinkName = sinkName;
        Message = message;
        Exception = exception;
        DroppedEntries = droppedEntries;
    }
}
```

- [ ] **Step 2: Create `KonzoleDiagnostics.cs`**

```csharp
namespace Rymote.Konzole.Diagnostics;

public static class KonzoleDiagnostics
{
    public static event EventHandler<SinkErrorEventArgs>? SinkError;

    private static readonly Dictionary<string, DateTimeOffset> LastFallbackEmitBySink = new();
    private static readonly object FallbackEmitGate = new();
    private static readonly TimeSpan FallbackEmitMinimumInterval = TimeSpan.FromMinutes(1);

    public static void ReportSinkError(SinkErrorEventArgs eventArgs)
    {
        EventHandler<SinkErrorEventArgs>? handler = SinkError;
        if (handler != null)
        {
            handler.Invoke(null, eventArgs);
            return;
        }

        if (!ShouldEmitFallback(eventArgs.SinkName)) return;

        Console.Error.WriteLine(eventArgs.Exception != null
            ? $"[Konzole/{eventArgs.SinkName}] {eventArgs.Message}: {eventArgs.Exception.Message}"
            : $"[Konzole/{eventArgs.SinkName}] {eventArgs.Message}");
    }

    private static bool ShouldEmitFallback(string sinkName)
    {
        lock (FallbackEmitGate)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (LastFallbackEmitBySink.TryGetValue(sinkName, out DateTimeOffset previousEmit)
                && now - previousEmit < FallbackEmitMinimumInterval)
            {
                return false;
            }
            LastFallbackEmitBySink[sinkName] = now;
            return true;
        }
    }
}
```

---

### Task 22: Add `FakeSink` test helper + rewrite `SinkBase` with channel + worker (TDD)

**Files:**
- Modify: `Rymote.Konzole/Sinks/SinkBase.cs`
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeSink.cs`
- Create: `Rymote.Konzole.Tests/Infrastructure/FakeSinkOptions.cs`
- Create: `Rymote.Konzole.Tests/Dispatch/SinkBaseBackpressureTests.cs`

- [ ] **Step 1: Create `FakeSinkOptions.cs`**

```csharp
using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeSinkOptions : SinkOptionsBase
{
    public TimeSpan WriteDelay { get; init; } = TimeSpan.Zero;
    public bool ThrowOnWrite { get; init; }
}
```

- [ ] **Step 2: Create `FakeSink.cs`**

```csharp
using System.Collections.Concurrent;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Tests.Infrastructure;

public sealed class FakeSink : SinkBase<FakeSinkOptions>
{
    private readonly ConcurrentQueue<LogEntry> _capturedEntries = new();

    public FakeSink(FakeSinkOptions options) : base(options) { }

    public override string Name => "Fake";

    public IReadOnlyList<LogEntry> CapturedEntries => _capturedEntries.ToArray();

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(useEmojis: false);

    protected override async ValueTask WriteBatchAsync(
        IReadOnlyList<LogEntry> batch,
        CancellationToken cancellationToken)
    {
        if (Options.WriteDelay > TimeSpan.Zero)
            await Task.Delay(Options.WriteDelay, cancellationToken);

        if (Options.ThrowOnWrite)
            throw new InvalidOperationException("FakeSink configured to throw.");

        foreach (LogEntry entry in batch)
            _capturedEntries.Enqueue(entry);
    }
}
```

- [ ] **Step 3: Write failing tests**

Create `Rymote.Konzole.Tests/Dispatch/SinkBaseBackpressureTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class SinkBaseBackpressureTests
{
    [Fact]
    public async Task TryEnqueue_DeliversEntriesToWorker_AndFlushDrains()
    {
        FakeSinkOptions sinkOptions = new() { MaxQueueSize = 100, ShutdownTimeout = TimeSpan.FromSeconds(2) };
        await using FakeSink fakeSink = new(sinkOptions);

        for (int index = 0; index < 25; index++)
        {
            fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = $"entry-{index}" });
        }

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.Equal(25, fakeSink.CapturedEntries.Count);
    }

    [Fact]
    public async Task TryEnqueue_FiltersBelowMinimumLevel()
    {
        FakeSinkOptions sinkOptions = new() { MinimumLevel = LogLevel.Warning };
        await using FakeSink fakeSink = new(sinkOptions);

        fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "ignored" });
        fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Warning, Message = "kept" });

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.Single(fakeSink.CapturedEntries);
        Assert.Equal("kept", fakeSink.CapturedEntries[0].Message);
    }

    [Fact]
    public async Task TryEnqueue_DropsOldest_WhenQueueFull()
    {
        FakeSinkOptions sinkOptions = new()
        {
            MaxQueueSize = 4,
            WriteDelay = TimeSpan.FromMilliseconds(50)
        };
        await using FakeSink fakeSink = new(sinkOptions);

        for (int index = 0; index < 200; index++)
        {
            fakeSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = $"entry-{index}" });
        }

        await fakeSink.FlushAsync(CancellationToken.None);

        Assert.True(fakeSink.CapturedEntries.Count < 200, "DropOldest should have shed entries under pressure.");
    }
}
```

- [ ] **Step 4: Replace `SinkBase.cs`**

```csharp
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public abstract class SinkBase<TOptions> : ISink
    where TOptions : SinkOptionsBase
{
    protected TOptions Options { get; }
    protected ILogFormatter Formatter { get; }
    protected FormatterContext FormatterContext { get; }

    private readonly Channel<LogEntry> _channel;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private int _disposed;

    protected SinkBase(TOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Formatter = options.Formatter ?? CreateDefaultFormatter();
        FormatterContext = options.BuildFormatterContext();

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

    protected virtual int BatchSize => 1;

    public void TryEnqueue(LogEntry entry)
    {
        if (entry.Level < Options.MinimumLevel) return;
        _channel.Writer.TryWrite(entry);
    }

    public virtual async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        while (_channel.Reader.Count > 0)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    protected abstract ILogFormatter CreateDefaultFormatter();
    protected abstract ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken);

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        List<LogEntry> batchBuffer = new(BatchSize);

        try
        {
            await foreach (LogEntry entry in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                batchBuffer.Add(entry);

                while (batchBuffer.Count < BatchSize && _channel.Reader.TryRead(out LogEntry? next))
                {
                    batchBuffer.Add(next);
                }

                try
                {
                    await WriteBatchAsync(batchBuffer, cancellationToken);
                }
                catch (Exception writeException) when (writeException is not OperationCanceledException)
                {
                    KonzoleDiagnostics.ReportSinkError(
                        new SinkErrorEventArgs(Name, "Sink write failed", writeException, batchBuffer.Count));
                }

                batchBuffer.Clear();
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _channel.Writer.TryComplete();

        try
        {
            await _workerTask.WaitAsync(Options.ShutdownTimeout);
        }
        catch (TimeoutException)
        {
            _shutdownTokenSource.Cancel();
            try { await _workerTask; } catch { }
        }

        _shutdownTokenSource.Dispose();
    }

    void IDisposable.Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

> **End of Phase 4.** Concrete sinks still reference old base/options shapes and will not compile. Continue to Phase 5.

---

## Phase 5 — File rolling strategies + `FileSink` rewrite

### Task 23: Create `FileRollingPolicy` enum and `IFileRollingStrategy` interface

**Files:**
- Create: `Rymote.Konzole/Sinks/Files/FileRollingPolicy.cs`
- Create: `Rymote.Konzole/Sinks/Files/IFileRollingStrategy.cs`

- [ ] **Step 1: Create `FileRollingPolicy.cs`**

```csharp
namespace Rymote.Konzole.Sinks.Files;

public enum FileRollingPolicy
{
    SizeOnly,
    DateOnly,
    DateThenSize
}
```

- [ ] **Step 2: Create `IFileRollingStrategy.cs`**

```csharp
namespace Rymote.Konzole.Sinks.Files;

internal interface IFileRollingStrategy
{
    string ResolveActivePath(string basePath, DateTimeOffset now);
    bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now);
    void Roll(string basePath, int maxFiles, DateTimeOffset now);
}
```

---

### Task 24: TDD `SizeOnlyRollingStrategy`

**Files:**
- Create: `Rymote.Konzole/Sinks/Files/SizeOnlyRollingStrategy.cs`
- Create: `Rymote.Konzole.Tests/Sinks/SizeOnlyRollingStrategyTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class SizeOnlyRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly DateTimeOffset _now = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

    public SizeOnlyRollingStrategyTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"konzole-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Fact]
    public void ResolveActivePath_AlwaysReturnsBasePath()
    {
        SizeOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");

        Assert.Equal(basePath, strategy.ResolveActivePath(basePath, _now));
    }

    [Fact]
    public void ShouldRoll_TrueWhenPendingExceedsCap()
    {
        SizeOnlyRollingStrategy strategy = new() { MaxFileSize = 100 };
        string activePath = Path.Combine(_temporaryDirectory, "app.log");

        Assert.False(strategy.ShouldRoll(activePath, currentSize: 50, pendingBytes: 40, _now));
        Assert.True(strategy.ShouldRoll(activePath, currentSize: 50, pendingBytes: 60, _now));
    }

    [Fact]
    public void Roll_ShiftsExistingFiles_AndDropsBeyondMaxFiles()
    {
        SizeOnlyRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");

        File.WriteAllText(basePath, "active");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.1.log"), "rotation-1");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.2.log"), "rotation-2");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.3.log"), "rotation-3");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app.4.log"), "rotation-4");

        strategy.Roll(basePath, maxFiles: 5, _now);

        Assert.False(File.Exists(basePath));
        Assert.Equal("active",     File.ReadAllText(Path.Combine(_temporaryDirectory, "app.1.log")));
        Assert.Equal("rotation-1", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.2.log")));
        Assert.Equal("rotation-2", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.3.log")));
        Assert.Equal("rotation-3", File.ReadAllText(Path.Combine(_temporaryDirectory, "app.4.log")));
        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app.5.log")));
    }
}
```

- [ ] **Step 2: Create `SizeOnlyRollingStrategy.cs`**

```csharp
namespace Rymote.Konzole.Sinks.Files;

internal sealed class SizeOnlyRollingStrategy : IFileRollingStrategy
{
    public long MaxFileSize { get; init; } = 10 * 1024 * 1024;

    public string ResolveActivePath(string basePath, DateTimeOffset now) => basePath;

    public bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now) =>
        currentSize + pendingBytes > MaxFileSize;

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);

        string PathForIndex(int rotationIndex) =>
            rotationIndex == 0
                ? basePath
                : Path.Combine(directoryPath, $"{fileNameWithoutExtension}.{rotationIndex}{fileExtension}");

        int oldestIndexToDrop = maxFiles - 1;
        string oldestPath = PathForIndex(oldestIndexToDrop);
        if (File.Exists(oldestPath))
            File.Delete(oldestPath);

        for (int rotationIndex = oldestIndexToDrop - 1; rotationIndex >= 1; rotationIndex--)
        {
            string sourcePath = PathForIndex(rotationIndex);
            string destinationPath = PathForIndex(rotationIndex + 1);
            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
        }

        if (File.Exists(basePath))
            File.Move(basePath, PathForIndex(1));
    }
}
```

---

### Task 25: TDD `DateOnlyRollingStrategy`

**Files:**
- Create: `Rymote.Konzole/Sinks/Files/DateOnlyRollingStrategy.cs`
- Create: `Rymote.Konzole.Tests/Sinks/DateOnlyRollingStrategyTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class DateOnlyRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public DateOnlyRollingStrategyTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"konzole-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Fact]
    public void ResolveActivePath_EmbedsDate()
    {
        DateOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        string activePath = strategy.ResolveActivePath(basePath, moment);

        Assert.Equal(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"), activePath);
    }

    [Fact]
    public void ShouldRoll_TrueWhenDateDiffersFromActivePath()
    {
        DateOnlyRollingStrategy strategy = new();
        string activePath = Path.Combine(_temporaryDirectory, "app-2026-05-17.log");

        Assert.False(strategy.ShouldRoll(activePath, 0, 0, new DateTimeOffset(2026, 5, 17, 23, 59, 0, TimeSpan.Zero)));
        Assert.True(strategy.ShouldRoll(activePath, 0, 0,  new DateTimeOffset(2026, 5, 18,  0,  0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Roll_DeletesFilesOlderThanMaxFilesDays()
    {
        DateOnlyRollingStrategy strategy = new();
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-10.log"), "old");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-15.log"), "recent");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-16.log"), "yesterday");

        strategy.Roll(basePath, maxFiles: 3, new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-10.log")));
        Assert.True (File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-15.log")));
        Assert.True (File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-16.log")));
    }
}
```

- [ ] **Step 2: Create `DateOnlyRollingStrategy.cs`**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rymote.Konzole.Sinks.Files;

internal sealed class DateOnlyRollingStrategy : IFileRollingStrategy
{
    private static readonly Regex DatedFileRegex = new(@"^(?<stem>.+)-(?<date>\d{4}-\d{2}-\d{2})(?<ext>\.[^.]+)$",
        RegexOptions.Compiled);

    public string ResolveActivePath(string basePath, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        string dateStamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}{fileExtension}");
    }

    public bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now)
    {
        DateTimeOffset? activeDate = ExtractDate(activePath);
        if (activeDate == null) return true;
        return activeDate.Value.Date != now.Date;
    }

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        DateTimeOffset cutoff = now.AddDays(-(maxFiles - 1)).Date;

        foreach (string candidatePath in Directory.EnumerateFiles(directoryPath, $"{fileNameWithoutExtension}-*{fileExtension}"))
        {
            DateTimeOffset? candidateDate = ExtractDate(candidatePath);
            if (candidateDate != null && candidateDate.Value.Date < cutoff)
            {
                File.Delete(candidatePath);
            }
        }
    }

    private static DateTimeOffset? ExtractDate(string path)
    {
        string fileName = Path.GetFileName(path);
        Match match = DatedFileRegex.Match(fileName);
        if (!match.Success) return null;
        return DateTimeOffset.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}
```

---

### Task 26: TDD `DateThenSizeRollingStrategy`

**Files:**
- Create: `Rymote.Konzole/Sinks/Files/DateThenSizeRollingStrategy.cs`
- Create: `Rymote.Konzole.Tests/Sinks/DateThenSizeRollingStrategyTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class DateThenSizeRollingStrategyTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public DateThenSizeRollingStrategyTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"konzole-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Fact]
    public void ResolveActivePath_EmbedsDate()
    {
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"),
            strategy.ResolveActivePath(basePath, moment));
    }

    [Fact]
    public void ShouldRoll_TrueOnSizeCap_EvenWithinSameDay()
    {
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string activePath = Path.Combine(_temporaryDirectory, "app-2026-05-17.log");
        DateTimeOffset sameDayMoment = new(2026, 5, 17, 14, 0, 0, TimeSpan.Zero);

        Assert.True(strategy.ShouldRoll(activePath, currentSize: 90, pendingBytes: 20, sameDayMoment));
    }

    [Fact]
    public void Roll_OnSizeCap_ShiftsSameDayFiles()
    {
        DateThenSizeRollingStrategy strategy = new() { MaxFileSize = 100 };
        string basePath = Path.Combine(_temporaryDirectory, "app.log");
        DateTimeOffset moment = new(2026, 5, 17, 14, 0, 0, TimeSpan.Zero);

        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.log"), "active-day");
        File.WriteAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.1.log"), "first-rotation");

        strategy.Roll(basePath, maxFiles: 5, moment);

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "app-2026-05-17.log")));
        Assert.Equal("active-day",     File.ReadAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.1.log")));
        Assert.Equal("first-rotation", File.ReadAllText(Path.Combine(_temporaryDirectory, "app-2026-05-17.2.log")));
    }
}
```

- [ ] **Step 2: Create `DateThenSizeRollingStrategy.cs`**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Rymote.Konzole.Sinks.Files;

internal sealed class DateThenSizeRollingStrategy : IFileRollingStrategy
{
    public long MaxFileSize { get; init; } = 10 * 1024 * 1024;

    private static readonly Regex DatedFileRegex = new(@"^(?<stem>.+)-(?<date>\d{4}-\d{2}-\d{2})(?<ext>\.[^.]+)$",
        RegexOptions.Compiled);

    public string ResolveActivePath(string basePath, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        string dateStamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}{fileExtension}");
    }

    public bool ShouldRoll(string activePath, long currentSize, long pendingBytes, DateTimeOffset now)
    {
        DateTimeOffset? activeDate = ExtractActiveDate(activePath);
        if (activeDate != null && activeDate.Value.Date != now.Date) return true;
        return currentSize + pendingBytes > MaxFileSize;
    }

    public void Roll(string basePath, int maxFiles, DateTimeOffset now)
    {
        string directoryPath = Path.GetDirectoryName(basePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
        string fileExtension = Path.GetExtension(basePath);
        string dateStamp = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string PathForIndex(int rotationIndex) =>
            rotationIndex == 0
                ? Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}{fileExtension}")
                : Path.Combine(directoryPath, $"{fileNameWithoutExtension}-{dateStamp}.{rotationIndex}{fileExtension}");

        int oldestIndexToDrop = maxFiles - 1;
        string oldestPath = PathForIndex(oldestIndexToDrop);
        if (File.Exists(oldestPath)) File.Delete(oldestPath);

        for (int rotationIndex = oldestIndexToDrop - 1; rotationIndex >= 1; rotationIndex--)
        {
            string sourcePath = PathForIndex(rotationIndex);
            string destinationPath = PathForIndex(rotationIndex + 1);
            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
        }

        string activeForToday = PathForIndex(0);
        if (File.Exists(activeForToday))
            File.Move(activeForToday, PathForIndex(1));

        DateTimeOffset cutoff = now.AddDays(-(maxFiles - 1)).Date;
        foreach (string candidatePath in Directory.EnumerateFiles(directoryPath, $"{fileNameWithoutExtension}-*{fileExtension}"))
        {
            DateTimeOffset? candidateDate = ExtractActiveDate(candidatePath);
            if (candidateDate != null && candidateDate.Value.Date < cutoff)
                File.Delete(candidatePath);
        }
    }

    private static DateTimeOffset? ExtractActiveDate(string path)
    {
        string fileName = Path.GetFileName(path);
        Match match = DatedFileRegex.Match(fileName);
        if (!match.Success) return null;
        return DateTimeOffset.ParseExact(match.Groups["date"].Value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }
}
```

---

### Task 27: Rewrite `FileSink` using the strategies

**Files:**
- Modify: `Rymote.Konzole/Sinks/FileSink.cs`
- Create: `Rymote.Konzole.Tests/Sinks/FileSinkRotationTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Sinks.Files;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class FileSinkRotationTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public FileSinkRotationTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), $"konzole-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    [Fact]
    public async Task SizeOnly_RotatesWhenFileSizeExceeded()
    {
        string logFilePath = Path.Combine(_temporaryDirectory, "app.log");
        FileSinkOptions options = new()
        {
            FilePath = logFilePath,
            RollingPolicy = FileRollingPolicy.SizeOnly,
            MaxFileSize = 200,
            MaxFiles = 3,
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            FlushInterval = TimeSpan.Zero
        };

        await using (FileSink fileSink = new(options))
        {
            for (int index = 0; index < 50; index++)
            {
                fileSink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = new string('x', 20) });
            }
            await fileSink.FlushAsync(CancellationToken.None);
        }

        Assert.True(File.Exists(logFilePath));
        Assert.True(File.Exists(Path.Combine(_temporaryDirectory, "app.1.log")));
    }
}
```

- [ ] **Step 2: Replace `FileSink.cs`**

```csharp
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Files;

namespace Rymote.Konzole.Sinks;

public sealed class FileSink : SinkBase<FileSinkOptions>
{
    private readonly IFileRollingStrategy _rollingStrategy;
    private readonly Func<DateTimeOffset> _clock;
    private readonly string _basePath;

    private StreamWriter? _streamWriter;
    private string _activeFilePath = string.Empty;
    private long _currentFileSize;
    private DateTimeOffset _lastFlush = DateTimeOffset.MinValue;

    public FileSink(FileSinkOptions options) : this(options, () => DateTimeOffset.UtcNow) { }

    public FileSink(FileSinkOptions options, Func<DateTimeOffset> clock) : base(options)
    {
        _clock = clock;
        _basePath = ResolveBasePath(options);
        EnsureDirectoryExists(_basePath);
        _rollingStrategy = BuildStrategy(options);
        OpenActiveWriter(_clock());
    }

    public override string Name => "File";

    protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();

    protected override async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string line = Formatter.Format(entry, FormatterContext);
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            DateTimeOffset now = _clock();
            if (_rollingStrategy.ShouldRoll(_activeFilePath, _currentFileSize, byteCount, now))
            {
                await CloseWriterAsync();
                _rollingStrategy.Roll(_basePath, Options.MaxFiles, now);
                OpenActiveWriter(now);
            }

            await _streamWriter!.WriteLineAsync(line.AsMemory(), cancellationToken);
            _currentFileSize += byteCount;
        }

        if (Options.FlushInterval == TimeSpan.Zero || _clock() - _lastFlush >= Options.FlushInterval)
        {
            await _streamWriter!.FlushAsync(cancellationToken);
            _lastFlush = _clock();
        }
    }

    public override async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        await base.FlushAsync(cancellationToken);
        if (_streamWriter != null)
            await _streamWriter.FlushAsync(cancellationToken);
    }

    private void OpenActiveWriter(DateTimeOffset now)
    {
        _activeFilePath = _rollingStrategy.ResolveActivePath(_basePath, now);
        EnsureDirectoryExists(_activeFilePath);
        _streamWriter = new StreamWriter(_activeFilePath, append: true, System.Text.Encoding.UTF8) { AutoFlush = false };
        _currentFileSize = File.Exists(_activeFilePath) ? new FileInfo(_activeFilePath).Length : 0;
    }

    private async Task CloseWriterAsync()
    {
        if (_streamWriter == null) return;
        await _streamWriter.FlushAsync();
        await _streamWriter.DisposeAsync();
        _streamWriter = null;
    }

    private static string ResolveBasePath(FileSinkOptions options) =>
        string.IsNullOrEmpty(options.FilePath)
            ? Path.Combine(AppContext.BaseDirectory, "logs", "konzole.log")
            : options.FilePath;

    private static void EnsureDirectoryExists(string filePath)
    {
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    private static IFileRollingStrategy BuildStrategy(FileSinkOptions options) => options.RollingPolicy switch
    {
        FileRollingPolicy.DateOnly     => new DateOnlyRollingStrategy(),
        FileRollingPolicy.DateThenSize => new DateThenSizeRollingStrategy { MaxFileSize = options.MaxFileSize },
        _                              => new SizeOnlyRollingStrategy    { MaxFileSize = options.MaxFileSize }
    };
}
```

---

## Phase 6 — HTTP sink base + concrete HTTP sinks

### Task 28: TDD `HttpSinkBase` retry semantics

**Files:**
- Create: `Rymote.Konzole/Sinks/Http/HttpSinkBase.cs`
- Create: `Rymote.Konzole.Tests/Sinks/HttpSinkRetryTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Rymote.Konzole.Tests/Sinks/HttpSinkRetryTests.cs`:

```csharp
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class HttpSinkRetryTests
{
    private sealed class ProbeSinkOptions : HttpSinkOptionsBase { }

    private sealed class ProbeHttpSink : HttpSinkBase<ProbeSinkOptions>
    {
        public string Endpoint { get; }
        public ProbeHttpSink(ProbeSinkOptions options, HttpClient httpClient, string endpoint)
            : base(options, httpClient) { Endpoint = endpoint; }
        public override string Name => "Probe";
        protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();
        protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
            => new(HttpMethod.Post, Endpoint) { Content = new StringContent($"{batch.Count}") };
    }

    [Fact]
    public async Task TransientFailures_AreRetriedUpToMaxAttempts_ThenSucceed()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };
        await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");

        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "retry-me" });
        await sink.FlushAsync(CancellationToken.None);

        Assert.Equal(3, handler.RecordedRequests.Count);
    }

    [Fact]
    public async Task NonRetryable4xx_DropsBatchImmediately()
    {
        FakeHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.BadRequest));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 5,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };

        int reportedDropped = 0;
        void ErrorHandler(object? sender, SinkErrorEventArgs args) => reportedDropped += args.DroppedEntries;
        KonzoleDiagnostics.SinkError += ErrorHandler;

        try
        {
            await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");
            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "bad" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            KonzoleDiagnostics.SinkError -= ErrorHandler;
        }

        Assert.Equal(1, handler.RecordedRequests.Count);
        Assert.Equal(1, reportedDropped);
    }

    [Fact]
    public async Task TooManyRequests_HonorsRetryAfterHeader()
    {
        FakeHttpMessageHandler handler = new();
        HttpResponseMessage throttled = new(HttpStatusCode.TooManyRequests);
        throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(50));
        handler.EnqueueResponse(throttled);
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using HttpClient httpClient = new(handler);
        ProbeSinkOptions options = new()
        {
            MaxRetryAttempts = 3,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            MaxRetryDelay = TimeSpan.FromMilliseconds(5),
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            BatchSize = 1
        };
        await using ProbeHttpSink sink = new(options, httpClient, "https://example.test/log");

        DateTimeOffset startedAt = DateTimeOffset.UtcNow;
        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "throttled" });
        await sink.FlushAsync(CancellationToken.None);
        TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;

        Assert.Equal(2, handler.RecordedRequests.Count);
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(40), $"Expected to wait Retry-After; elapsed={elapsed}");
    }
}
```

- [ ] **Step 2: Create `HttpSinkBase.cs`**

```csharp
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Diagnostics;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Sinks.Http;

public abstract class HttpSinkBase<TOptions> : SinkBase<TOptions>
    where TOptions : HttpSinkOptionsBase
{
    protected HttpClient HttpClient { get; }

    protected override int BatchSize => Options.BatchSize;

    protected HttpSinkBase(TOptions options, HttpClient httpClient) : base(options)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (HttpClient.Timeout == System.Threading.Timeout.InfiniteTimeSpan || HttpClient.Timeout == TimeSpan.Zero)
            HttpClient.Timeout = options.RequestTimeout;
    }

    protected abstract HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch);

    protected sealed override async ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        int attemptIndex = 0;

        while (true)
        {
            HttpRequestMessage request = BuildRequest(batch);
            HttpResponseMessage? response = null;
            Exception? transientException = null;

            try
            {
                response = await HttpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException requestException) { transientException = requestException; }
            catch (TaskCanceledException taskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                transientException = taskCanceledException;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                response.Dispose();
                return;
            }

            bool isRetryable = transientException != null
                || (response != null && IsRetryableStatus(response.StatusCode));

            if (!isRetryable)
            {
                int statusCode = response != null ? (int)response.StatusCode : 0;
                response?.Dispose();
                KonzoleDiagnostics.ReportSinkError(new SinkErrorEventArgs(
                    Name,
                    $"Non-retryable response (status={statusCode}); dropped {batch.Count} entries.",
                    droppedEntries: batch.Count));
                return;
            }

            if (attemptIndex >= Options.MaxRetryAttempts)
            {
                int statusCode = response != null ? (int)response.StatusCode : 0;
                response?.Dispose();
                KonzoleDiagnostics.ReportSinkError(new SinkErrorEventArgs(
                    Name,
                    $"Exhausted {Options.MaxRetryAttempts} retries (last status={statusCode}); dropped {batch.Count} entries.",
                    transientException,
                    droppedEntries: batch.Count));
                return;
            }

            TimeSpan delay = ComputeBackoffDelay(attemptIndex, response);
            response?.Dispose();
            await Task.Delay(delay, cancellationToken);
            attemptIndex++;
        }
    }

    private static bool IsRetryableStatus(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout
        || statusCode == HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;

    private TimeSpan ComputeBackoffDelay(int attemptIndex, HttpResponseMessage? response)
    {
        if (response?.StatusCode == HttpStatusCode.TooManyRequests
            && response.Headers.RetryAfter?.Delta is TimeSpan retryAfter)
        {
            return retryAfter;
        }

        double multiplier = Math.Pow(2, attemptIndex);
        TimeSpan exponentialDelay = TimeSpan.FromMilliseconds(Options.BaseRetryDelay.TotalMilliseconds * multiplier);
        return exponentialDelay > Options.MaxRetryDelay ? Options.MaxRetryDelay : exponentialDelay;
    }
}
```

---

### Task 29: Rewrite `RemoteSink`

**Files:**
- Modify: `Rymote.Konzole/Sinks/RemoteSink.cs`

- [ ] **Step 1: Replace contents**

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class RemoteSink : HttpSinkBase<RemoteSinkOptions>
{
    public RemoteSink(RemoteSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (!string.IsNullOrEmpty(options.RemoteApiKey))
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.RemoteApiKey);
    }

    public override string Name => "Remote";

    protected override ILogFormatter CreateDefaultFormatter() => new JsonFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        object batchEnvelope = new
        {
            timestamp = DateTimeOffset.UtcNow,
            source = Environment.MachineName,
            entries = batch.Select(entry => JsonSerializer.Deserialize<JsonElement>(Formatter.Format(entry, FormatterContext)))
        };

        string jsonPayload = JsonSerializer.Serialize(batchEnvelope);
        return new HttpRequestMessage(HttpMethod.Post, Options.RemoteEndpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }
}
```

> **Note:** The `using Microsoft.Extensions.Http;` import isn't strictly needed (the factory interface is in `System.Net.Http`), but the package reference must be present for hosts to construct the factory. The builder in Phase 8 wires this up.

---

### Task 30: Rewrite `DiscordSink`

**Files:**
- Modify: `Rymote.Konzole/Sinks/DiscordSink.cs`

- [ ] **Step 1: Replace contents**

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class DiscordSink : HttpSinkBase<DiscordSinkOptions>
{
    public DiscordSink(DiscordSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (string.IsNullOrEmpty(options.WebhookUrl))
            throw new ArgumentException("Discord webhook URL is required.", nameof(options));
    }

    public override string Name => "Discord";

    protected override ILogFormatter CreateDefaultFormatter() => new DiscordFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        LogEntry entry = batch[0];
        object payload = Options.UseEmbeds ? BuildEmbedPayload(entry) : BuildPlainPayload(entry);
        string jsonPayload = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(HttpMethod.Post, Options.WebhookUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }

    private object BuildPlainPayload(LogEntry entry) => new
    {
        username = Options.Username,
        avatar_url = Options.AvatarUrl,
        content = FormatterHelpers.TruncateMessage(Formatter.Format(entry, FormatterContext), Options.MaxMessageLength)
    };

    private object BuildEmbedPayload(LogEntry entry)
    {
        int embedColor = entry.Tag.HasValue
            ? Options.TagEmbedColors.GetValueOrDefault(entry.Tag.Value, 0x808080)
            : Options.LevelEmbedColors.GetValueOrDefault(entry.Level, 0x808080);

        string title = $"{LogIcon.GetIcon(entry.Tag ?? default)} {entry.Tag?.ToString() ?? entry.Level.ToString()}";
        if (!entry.Tag.HasValue) title = $"{LogIcon.GetIcon(entry.Level)} {entry.Level}";

        return new
        {
            username = Options.Username,
            avatar_url = Options.AvatarUrl,
            embeds = new[]
            {
                new
                {
                    title,
                    description = FormatterHelpers.TruncateMessage(entry.Message, Options.MaxMessageLength),
                    color = embedColor,
                    timestamp = entry.Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"),
                    footer = new { text = Environment.MachineName }
                }
            }
        };
    }
}
```

---

### Task 31: Rewrite `SlackSink`

**Files:**
- Modify: `Rymote.Konzole/Sinks/SlackSink.cs`

- [ ] **Step 1: Replace contents**

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks.Http;

namespace Rymote.Konzole.Sinks;

public sealed class SlackSink : HttpSinkBase<SlackSinkOptions>
{
    public SlackSink(SlackSinkOptions options, System.Net.Http.IHttpClientFactory httpClientFactory)
        : base(options, httpClientFactory.CreateClient(options.HttpClientName))
    {
        if (string.IsNullOrEmpty(options.WebhookUrl))
            throw new ArgumentException("Slack webhook URL is required.", nameof(options));
    }

    public override string Name => "Slack";

    protected override ILogFormatter CreateDefaultFormatter() => new SlackFormatter();

    protected override HttpRequestMessage BuildRequest(IReadOnlyList<LogEntry> batch)
    {
        LogEntry entry = batch[0];
        object payload = Options.UseAttachments ? BuildAttachmentPayload(entry) : BuildPlainPayload(entry);
        string jsonPayload = JsonSerializer.Serialize(payload);
        return new HttpRequestMessage(HttpMethod.Post, Options.WebhookUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
    }

    private object BuildPlainPayload(LogEntry entry) => new
    {
        channel = Options.Channel,
        username = Options.Username,
        icon_emoji = Options.IconEmoji,
        icon_url = Options.IconUrl,
        text = FormatterHelpers.TruncateMessage(Formatter.Format(entry, FormatterContext), Options.MaxMessageLength)
    };

    private object BuildAttachmentPayload(LogEntry entry)
    {
        string attachmentColor = entry.Tag.HasValue
            ? Options.TagAttachmentColors.GetValueOrDefault(entry.Tag.Value, "#808080")
            : Options.LevelAttachmentColors.GetValueOrDefault(entry.Level, "#808080");

        string label = entry.Tag?.ToString() ?? entry.Level.ToString();
        string icon = entry.Tag.HasValue ? LogIcon.GetIcon(entry.Tag.Value) : LogIcon.GetIcon(entry.Level);

        return new
        {
            channel = Options.Channel,
            username = Options.Username,
            icon_emoji = Options.IconEmoji,
            icon_url = Options.IconUrl,
            attachments = new[]
            {
                new
                {
                    fallback = $"{label}: {entry.Message}",
                    color = attachmentColor,
                    pretext = $"{icon} *{label}*",
                    text = FormatterHelpers.TruncateMessage(entry.Message, Options.MaxMessageLength),
                    ts = new DateTimeOffset(entry.Timestamp.DateTime, entry.Timestamp.Offset).ToUnixTimeSeconds(),
                    footer = Environment.MachineName,
                    mrkdwn_in = new[] { "pretext", "text" }
                }
            }
        };
    }
}
```

---

## Phase 7 — `ConsoleSink` rewrite

### Task 32: Rewrite `ConsoleSink`

**Files:**
- Modify: `Rymote.Konzole/Sinks/ConsoleSink.cs`
- Create: `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Xunit;

namespace Rymote.Konzole.Tests.Sinks;

public class ConsoleSinkTests
{
    [Fact]
    public async Task WritesToStandardOut_ForInformation()
    {
        StringWriter capturedStandardOut = new();
        TextWriter originalStandardOut = Console.Out;
        Console.SetOut(capturedStandardOut);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = false, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "hello-stdout" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetOut(originalStandardOut);
        }

        Assert.Contains("hello-stdout", capturedStandardOut.ToString());
    }

    [Fact]
    public async Task WritesToStandardError_ForErrorLevel()
    {
        StringWriter capturedStandardError = new();
        TextWriter originalStandardError = Console.Error;
        Console.SetError(capturedStandardError);

        try
        {
            ConsoleSinkOptions options = new() { UseColors = false, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
            await using ConsoleSink sink = new(options);

            sink.TryEnqueue(new LogEntry { Level = LogLevel.Error, Message = "hello-stderr" });
            await sink.FlushAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetError(originalStandardError);
        }

        Assert.Contains("hello-stderr", capturedStandardError.ToString());
    }
}
```

- [ ] **Step 2: Replace `ConsoleSink.cs`**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public sealed class ConsoleSink : SinkBase<ConsoleSinkOptions>
{
    private readonly Lock _consoleGate = new();

    public ConsoleSink(ConsoleSinkOptions options) : base(options) { }

    public override string Name => "Console";

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(Options.UseEmojis);

    protected override ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string rendered = Formatter.Format(entry, FormatterContext);
            TextWriter destination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;

            lock (_consoleGate)
            {
                if (!Options.UseColors)
                {
                    destination.WriteLine(rendered);
                    continue;
                }

                ConsoleColor originalForeground = Console.ForegroundColor;
                ConsoleColor originalBackground = Console.BackgroundColor;
                try
                {
                    ApplyColor(entry);
                    destination.WriteLine(rendered);
                }
                finally
                {
                    Console.ForegroundColor = originalForeground;
                    Console.BackgroundColor = originalBackground;
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    private void ApplyColor(LogEntry entry)
    {
        if (entry.Tag.HasValue && Options.TagColors.TryGetValue(entry.Tag.Value, out ConsoleColor tagColor))
        {
            Console.ForegroundColor = tagColor;
            return;
        }

        if (Options.LevelColors.TryGetValue(entry.Level, out ConsoleColor levelColor))
        {
            Console.ForegroundColor = levelColor;
        }

        if (entry.Level == LogLevel.Critical)
        {
            Console.BackgroundColor = Options.CriticalBackgroundColor;
        }
    }
}
```

> **End of Phase 7.** Logger/provider/builder still reference removed types. Phase 8 makes it green.

---

## Phase 8 — Logger, provider, builder, extensions (green at end)

### Task 33: Rewrite `KonzoleLogger`

**Files:**
- Modify: `Rymote.Konzole/KonzoleLogger.cs`
- Create: `Rymote.Konzole.Tests/Dispatch/KonzoleLoggerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class KonzoleLoggerTests
{
    [Fact]
    public async Task IsEnabled_ReturnsTrue_OnlyWhenSomeSinkAcceptsLevel()
    {
        await using FakeSink informationSink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Information });
        await using FakeSink errorOnlySink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Error });

        KonzoleLogger logger = new("Test.Category", new ISink[] { errorOnlySink });
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True (logger.IsEnabled(LogLevel.Error));

        KonzoleLogger broader = new("Test.Category", new ISink[] { informationSink, errorOnlySink });
        Assert.True (broader.IsEnabled(LogLevel.Information));
        Assert.True (broader.IsEnabled(LogLevel.Error));
    }

    [Fact]
    public async Task Log_DispatchesToAllSinks_WithTagFromScope()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        using (KonzoleScopeState.Push(new KonzoleScopeState { Tag = KonzoleTag.Success }))
        {
            logger.LogInformation("scoped");
        }

        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(KonzoleTag.Success, sink.CapturedEntries[0].Tag);
        Assert.Equal("scoped", sink.CapturedEntries[0].Message);
    }

    [Fact]
    public async Task Log_ExtractsStructuredProperties_FromState()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogInformation("user {UserId} did {Action}", 42, "login");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        LogEntry captured = sink.CapturedEntries[0];
        Assert.NotNull(captured.Properties);
        Assert.Equal(42, captured.Properties!["UserId"]);
        Assert.Equal("login", captured.Properties["Action"]);
    }
}
```

- [ ] **Step 2: Replace `KonzoleLogger.cs`**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

public sealed class KonzoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly IReadOnlyList<ISink> _sinks;

    public KonzoleLogger(string categoryName, IReadOnlyList<ISink> sinks)
    {
        _categoryName = categoryName;
        _sinks = sinks;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        if (state is KonzoleScopeState scopeState) return KonzoleScopeState.Push(scopeState);

        KonzoleScopeState? currentScope = KonzoleScopeState.Current;
        KonzoleScopeState wrappedScope = new()
        {
            Tag = currentScope?.Tag,
            TraceId = currentScope?.TraceId,
            SpanId = currentScope?.SpanId
        };
        return KonzoleScopeState.Push(wrappedScope);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        foreach (ISink sink in _sinks)
        {
            if (logLevel >= sink.MinimumLevel) return true;
        }
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        KonzoleScopeState? currentScope = KonzoleScopeState.Current;

        LogEntry logEntry = new()
        {
            Timestamp = DateTimeOffset.UtcNow,
            Level = logLevel,
            Tag = currentScope?.Tag,
            Message = message,
            Category = _categoryName,
            EventId = eventId,
            Exception = exception,
            Properties = ExtractProperties(state),
            Scope = currentScope?.Tag?.ToString(),
            TraceId = currentScope?.TraceId,
            SpanId = currentScope?.SpanId
        };

        foreach (ISink sink in _sinks)
        {
            sink.TryEnqueue(logEntry);
        }
    }

    private static IReadOnlyDictionary<string, object?>? ExtractProperties<TState>(TState state)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> structuredState) return null;

        Dictionary<string, object?> extracted = new(structuredState.Count);
        foreach (KeyValuePair<string, object?> pair in structuredState)
        {
            if (pair.Key == "{OriginalFormat}") continue;
            extracted[pair.Key] = pair.Value;
        }
        return extracted.Count == 0 ? null : extracted;
    }
}
```

---

### Task 34: Rewrite `KonzoleLoggerProvider`

**Files:**
- Modify: `Rymote.Konzole/KonzoleLoggerProvider.cs`
- Create: `Rymote.Konzole.Tests/Dispatch/GracefulShutdownTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Dispatch;

public class GracefulShutdownTests
{
    [Fact]
    public async Task DisposeAsync_FlushesAllSinks_BeforeReturning()
    {
        FakeSink sink = new(new FakeSinkOptions
        {
            ShutdownTimeout = TimeSpan.FromSeconds(2),
            WriteDelay = TimeSpan.FromMilliseconds(10)
        });

        KonzoleLoggerProvider provider = new(new ISink[] { sink });
        ILogger logger = provider.CreateLogger("Test.Category");

        for (int index = 0; index < 50; index++)
        {
            logger.LogInformation("entry-{Index}", index);
        }

        await provider.DisposeAsync();

        Assert.Equal(50, sink.CapturedEntries.Count);
    }
}
```

- [ ] **Step 2: Replace `KonzoleLoggerProvider.cs`**

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole;

[ProviderAlias("Konzole")]
public sealed class KonzoleLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, KonzoleLogger> _loggers = new();
    private readonly IReadOnlyList<ISink> _sinks;
    private int _disposed;

    public KonzoleLoggerProvider(IEnumerable<ISink> sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = sinks.ToArray();
        if (_sinks.Count == 0)
            throw new ArgumentException("At least one sink must be provided.", nameof(sinks));
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new KonzoleLogger(name, _sinks));

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        foreach (ISink sink in _sinks)
        {
            try { await sink.FlushAsync(CancellationToken.None); } catch { }
        }

        foreach (ISink sink in _sinks)
        {
            try { await sink.DisposeAsync(); } catch { }
        }

        _loggers.Clear();
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
```

---

### Task 35: Rewrite `KonzoleBuilder`

**Files:**
- Modify: `Rymote.Konzole/Configuration/KonzoleBuilder.cs`
- Create: `Rymote.Konzole.Tests/Configuration/KonzoleBuilderTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Extensions;
using Xunit;

namespace Rymote.Konzole.Tests.Configuration;

public class KonzoleBuilderTests
{
    [Fact]
    public void Build_Throws_WhenNoSinksConfigured()
    {
        ServiceCollection services = new();
        services.AddLogging();

        ILoggingBuilder loggingBuilder = new TestLoggingBuilder(services);
        KonzoleBuilder konzoleBuilder = new(loggingBuilder);

        Assert.Throws<InvalidOperationException>(() => konzoleBuilder.Build());
    }

    [Fact]
    public void Build_RegistersHttpClientFactory_WhenHttpSinkUsed()
    {
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.AddKonzole(konzole => konzole.AddRemoteSink("https://example.test/log"));
        });

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<System.Net.Http.IHttpClientFactory>());
    }

    private sealed class TestLoggingBuilder : ILoggingBuilder
    {
        public TestLoggingBuilder(IServiceCollection services) { Services = services; }
        public IServiceCollection Services { get; }
    }
}
```

- [ ] **Step 2: Replace `KonzoleBuilder.cs`**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Configuration;

public sealed class KonzoleBuilder
{
    private readonly ILoggingBuilder _loggingBuilder;
    private readonly List<ServiceDescriptor> _sinkDescriptors = new();
    private bool _hasHttpSink;

    public KonzoleBuilder(ILoggingBuilder loggingBuilder)
    {
        _loggingBuilder = loggingBuilder;
    }

    public KonzoleBuilder AddConsoleSink(Action<ConsoleSinkOptions>? configure = null)
    {
        ConsoleSinkOptions options = new();
        configure?.Invoke(options);
        _sinkDescriptors.Add(ServiceDescriptor.Singleton<ISink>(_ => new ConsoleSink(options)));
        return this;
    }

    public KonzoleBuilder AddFileSink(string? filePath = null, Action<FileSinkOptions>? configure = null)
    {
        FileSinkOptions options = new();
        if (!string.IsNullOrEmpty(filePath)) options.FilePath = filePath;
        configure?.Invoke(options);
        _sinkDescriptors.Add(ServiceDescriptor.Singleton<ISink>(_ => new FileSink(options)));
        return this;
    }

    public KonzoleBuilder AddRemoteSink(string endpoint, string? apiKey = null, Action<RemoteSinkOptions>? configure = null)
    {
        RemoteSinkOptions options = new() { RemoteEndpoint = endpoint, RemoteApiKey = apiKey };
        configure?.Invoke(options);
        AddHttpSink(serviceProvider => new RemoteSink(options, serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));
        return this;
    }

    public KonzoleBuilder AddDiscordSink(string webhookUrl, Action<DiscordSinkOptions>? configure = null)
    {
        DiscordSinkOptions options = new() { WebhookUrl = webhookUrl };
        configure?.Invoke(options);
        AddHttpSink(serviceProvider => new DiscordSink(options, serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));
        return this;
    }

    public KonzoleBuilder AddSlackSink(string webhookUrl, string? channel = null, Action<SlackSinkOptions>? configure = null)
    {
        SlackSinkOptions options = new() { WebhookUrl = webhookUrl, Channel = channel };
        configure?.Invoke(options);
        AddHttpSink(serviceProvider => new SlackSink(options, serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));
        return this;
    }

    public KonzoleBuilder AddSink(ISink sink)
    {
        _sinkDescriptors.Add(ServiceDescriptor.Singleton<ISink>(_ => sink));
        return this;
    }

    public KonzoleBuilder AddSink<TSink>() where TSink : class, ISink
    {
        _sinkDescriptors.Add(ServiceDescriptor.Singleton<ISink, TSink>());
        return this;
    }

    public void Build()
    {
        if (_sinkDescriptors.Count == 0)
            throw new InvalidOperationException("At least one sink must be configured.");

        if (_hasHttpSink)
            _loggingBuilder.Services.AddHttpClient();

        foreach (ServiceDescriptor descriptor in _sinkDescriptors)
            _loggingBuilder.Services.Add(descriptor);

        _loggingBuilder.Services.TryAddSingleton<KonzoleLoggerProvider>(
            serviceProvider => new KonzoleLoggerProvider(serviceProvider.GetServices<ISink>()));
        _loggingBuilder.Services.AddSingleton<ILoggerProvider>(
            serviceProvider => serviceProvider.GetRequiredService<KonzoleLoggerProvider>());
    }

    private void AddHttpSink(Func<IServiceProvider, ISink> factory)
    {
        _hasHttpSink = true;
        _sinkDescriptors.Add(ServiceDescriptor.Singleton<ISink>(factory));
    }
}
```

---

### Task 36: Rewrite `LoggingBuilderExtensions`

**Files:**
- Modify: `Rymote.Konzole/Extensions/LoggingBuilderExtensions.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder, Action<KonzoleBuilder> configure)
    {
        KonzoleBuilder konzoleBuilder = new(builder);
        configure(konzoleBuilder);
        konzoleBuilder.Build();
        return builder;
    }

    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder) =>
        builder.AddKonzole(konzoleBuilder => konzoleBuilder.AddConsoleSink(options =>
        {
            options.UseColors = true;
            options.UseEmojis = true;
        }));
}
```

(`AddKonzoleStdout`, `AddKonzoleFile`, `AddKonzoleRemote`, `AddKonzoleDiscord`, `AddKonzoleSlack` are removed.)

---

### Task 37: Rewrite `LoggerExtensions` (KonzoleScopeState helpers)

**Files:**
- Modify: `Rymote.Konzole/Extensions/LoggerExtensions.cs`
- Create: `Rymote.Konzole.Tests/Configuration/LoggerExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole;
using Rymote.Konzole.Extensions;
using Rymote.Konzole.Models;
using Rymote.Konzole.Sinks;
using Rymote.Konzole.Tests.Infrastructure;
using Xunit;

namespace Rymote.Konzole.Tests.Configuration;

public class LoggerExtensionsTests
{
    [Fact]
    public async Task LogSuccess_TagsEntryWithSuccess_AndLogsInformation()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogSuccess("operation done");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(LogLevel.Information, sink.CapturedEntries[0].Level);
        Assert.Equal(KonzoleTag.Success, sink.CapturedEntries[0].Tag);
    }

    [Fact]
    public async Task LogFatal_MapsToCritical_WithoutTag()
    {
        await using FakeSink sink = new(new FakeSinkOptions { MinimumLevel = LogLevel.Trace });
        KonzoleLogger logger = new("Test.Category", new ISink[] { sink });

        logger.LogFatal(new InvalidOperationException("boom"), "fatal");
        await sink.FlushAsync(CancellationToken.None);

        Assert.Single(sink.CapturedEntries);
        Assert.Equal(LogLevel.Critical, sink.CapturedEntries[0].Level);
        Assert.Null(sink.CapturedEntries[0].Tag);
    }
}
```

- [ ] **Step 2: Replace `LoggerExtensions.cs`**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Extensions;

public static class LoggerExtensions
{
    public static void LogSuccess(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Success, exception: null, message, arguments);

    public static void LogPending(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Pending, exception: null, message, arguments);

    public static void LogComplete(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Complete, exception: null, message, arguments);

    public static void LogNote(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Note, exception: null, message, arguments);

    public static void LogStart(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Start, exception: null, message, arguments);

    public static void LogPause(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Information, KonzoleTag.Pause, exception: null, message, arguments);

    public static void LogWatch(this ILogger logger, string message, params object?[] arguments) =>
        LogWithTag(logger, LogLevel.Debug, KonzoleTag.Watch, exception: null, message, arguments);

    public static void LogFatal(this ILogger logger, string message, params object?[] arguments) =>
        logger.LogCritical(message, arguments);

    public static void LogFatal(this ILogger logger, Exception exception, string message, params object?[] arguments) =>
        logger.LogCritical(exception, message, arguments);

    private static void LogWithTag(
        ILogger logger,
        LogLevel logLevel,
        KonzoleTag tag,
        Exception? exception,
        string message,
        params object?[] arguments)
    {
        using (KonzoleScopeState.Push(new KonzoleScopeState { Tag = tag }))
        {
            if (exception != null) logger.Log(logLevel, exception, message, arguments);
            else logger.Log(logLevel, message, arguments);
        }
    }
}
```

---

### Task 38: Green checkpoint — full build + test

- [ ] **Step 1: Build the solution**

```bash
dotnet build Rymote.Konzole.sln
```

Expected: 0 errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test Rymote.Konzole.sln
```

Expected: all tests pass.

- [ ] **Step 3: Commit the Phase 3-8 rewrite (ask user first)**

```bash
git add Rymote.Konzole/ Rymote.Konzole.Tests/
git commit -m "refactor: rewrite logger, sinks, formatters, and options around per-sink channels with KonzoleTag scope"
```

> **Reviewer note:** This commit is intentionally large — it spans cross-cutting type changes (KonzoleLogLevel removal, ILogFormatter signature change, ISink contract change, LogEntry record). Splitting it would leave intermediate commits with a red build. The design doc captures intent.

---

## Phase 9 — README + final polish

### Task 39: Write the real `README.md`

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Replace the placeholder with the full README**

```markdown
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

| Helper       | MEL level     | Tag                 |
|--------------|---------------|---------------------|
| `LogStart`   | `Information` | `KonzoleTag.Start`  |
| `LogPending` | `Information` | `KonzoleTag.Pending`|
| `LogSuccess` | `Information` | `KonzoleTag.Success`|
| `LogComplete`| `Information` | `KonzoleTag.Complete`|
| `LogNote`    | `Information` | `KonzoleTag.Note`   |
| `LogPause`   | `Information` | `KonzoleTag.Pause`  |
| `LogWatch`   | `Debug`       | `KonzoleTag.Watch`  |
| `LogFatal`   | `Critical`    | _(none — Critical already is fatal)_ |

## Writing a custom sink

```csharp
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
KonzoleDiagnostics.SinkError += (sender, args) =>
{
    metricsCollector.Increment("logging.sink.error", new { sink = args.SinkName });
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
```

- [ ] **Step 2: Commit (ask user first)**

```bash
git add README.md
git commit -m "docs: write Konzole README with quick-start, sinks, custom levels, migration notes"
```

---

## Self-review

I read the spec again with fresh eyes and checked the plan against it.

**1. Spec coverage**

| Spec section | Covered by |
|---|---|
| §3 Architecture & data flow | Task 22 (`SinkBase` channel+worker), Task 33 (`KonzoleLogger.Log`), Task 34 (`KonzoleLoggerProvider` graceful drain) |
| §4.1 Registration entry points | Tasks 35-36 |
| §4.2 Hybrid DI for sinks | Task 35 (`AddHttpSink` factory branch; `AddHttpClient` registration) |
| §4.3 `KonzoleScopeState`, helpers, `KonzoleTag` | Tasks 6, 7, 37 |
| §4.4 `LogEntry` record shape | Task 10 |
| §5 Formatter/sink decoupling | Tasks 9, 11-14 |
| §6.1 `ISink`, `SinkBase` | Tasks 20, 22 |
| §6.2 `ConsoleSink` | Task 32 |
| §6.3 `FileSink` + rolling | Tasks 23-27 |
| §6.4 HTTP sink base + retry | Tasks 28-31 |
| §6.5 File splits (`KonzoleScopeState`, `ExceptionJsonConverter`, deleting `KonzoleLogLevel`) | Tasks 7, 10, 12 |
| §7 Testing | Test tasks throughout each rewrite; `FakeClock`, `FakeSink`, `FakeHttpMessageHandler` (Tasks 4, 5, 22) |
| §8 Packaging | Task 2 |
| §9 Hygiene (`.gitignore`, deleted logs, restating-comment cleanup) | Task 1, and comment-stripping is implicit in each rewrite |
| §10 Migration impact | Task 39 (README migration section) |

No gaps.

**2. Placeholder scan**

- No `TBD`, `TODO`, or "implement later" markers.
- No "add error handling" hand-waves.
- Every code step shows the complete code.
- The few "similar to" notes (e.g., DateOnly/DateThenSize strategies share a regex pattern) repeat the code in each task.

**3. Type/method consistency**

Reviewed types and method names across tasks:

- `KonzoleTag` (Task 6) — used by Tasks 7, 8, 10, 11-14, 17, 19, 32, 33, 37, 39. Consistent.
- `KonzoleScopeState.Push` (Task 7) — used by Tasks 33, 37. Consistent.
- `KonzoleScopeState.Current` (Task 7) — used by Task 33. Consistent.
- `FormatterContext.BuildFormatterContext` is defined on `SinkOptionsBase` (Task 15) and consumed by `SinkBase<TOptions>` (Task 22). Consistent.
- `ISink.TryEnqueue` / `FlushAsync` (Task 20) — used by Tasks 22, 33, 34. Consistent.
- `HttpSinkBase.BuildRequest` (Task 28) — implemented by Tasks 29, 30, 31. Consistent.
- `FileRollingPolicy` (Task 23) — used by Tasks 18, 27. Consistent.
- `KonzoleDiagnostics.ReportSinkError` (Task 21) — used by Tasks 22, 28. Consistent.
- `SinkOptionsBase.MaxQueueSize` / `ShutdownTimeout` (Task 15) — used by Tasks 22, 27, 34. Consistent.
- `HttpSinkOptionsBase.MaxRetryAttempts` / `BaseRetryDelay` / `MaxRetryDelay` / `BatchSize` (Task 16) — used by Task 28. Consistent.

No drift detected.

**4. Other notes**

- The plan assumes `dotnet new xunit` produces a project targeting net10. With `--framework net10.0` flag it does.
- The `using Microsoft.Extensions.Http;` in `RemoteSink.cs` (Task 29) is not strictly necessary; the type lives in `System.Net.Http`. The note acknowledges this.
- `Lock` (Task 32) requires C# 13 / .NET 9+; project targets net10 so it's available.
- The `FormatterContext` for tests is created inline in test files; not all tests use the `BuildFormatterContext` helper. That's fine — tests are explicit about the configuration they need.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-17-konzole-overhaul.md`. Two execution options:

**1. Subagent-Driven (recommended)** — Dispatches a fresh subagent per task, reviews between tasks, fast iteration. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — Executes tasks in this session using `superpowers:executing-plans`, batched with checkpoints for review.

Which approach?

