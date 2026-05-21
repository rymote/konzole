# Level Label + Icon Toggle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Commit policy:** Owner has a global rule against autonomous commits. No commits during execution — owner reviews and commits at the end of the feature.

**Goal:** Add bracketed level/tag label segment (`[INFO]`, `[ERROR]`, `[SUCCESS]`, etc.) to ConsoleFormatter output, plus master toggles `ShowIcon` and `ShowLevelLabel` on `ConsoleSinkOptions`. Both default to `true`.

**Architecture:** New `ConsoleSegment.LevelLabel` enum value, two new bools on `ConsoleSinkOptions`, `ConsoleFormatter` constructor gains two default-true parameters, `ConsoleSink` injects dynamic foreground for the new segment in its palette builder. Label text comes from existing `LogIcon.GetFallbackIcon` (returns `"[INFO]"`, `"[SUCCESS]"`, etc.) — wrapped in `[level-label][...][/]` markup so inner brackets pass through as literal text via leniency.

**Tech Stack:** .NET 10, C#, xUnit. No new dependencies.

**Spec:** `docs/superpowers/specs/2026-05-21-level-label-design.md`

---

## File map

### Modified
- `Rymote.Konzole/Styling/ConsoleSegment.cs` (add `LevelLabel` enum value)
- `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs` (add `ShowIcon`, `ShowLevelLabel`, default style for `LevelLabel`)
- `Rymote.Konzole/Formatters/ConsoleFormatter.cs` (rewrite constructor + icon/label rendering)
- `Rymote.Konzole/Sinks/ConsoleSink.cs` (pass options to formatter, add `level-label` palette wiring, map enum)
- `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs` (5 new tests)
- `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs` (1 new test)
- `Rymote.Konzole/Rymote.Konzole.csproj` (bump version to `2.2.0`)

### Created
- None.

### Deleted
- None.

---

## Phase 1 — Foundational additions (green throughout)

### Task 1: Add `LevelLabel` to `ConsoleSegment` enum

**Files:**
- Modify: `Rymote.Konzole/Styling/ConsoleSegment.cs`

- [ ] **Step 1: Replace contents**

Replace `Rymote.Konzole/Styling/ConsoleSegment.cs` with:

```csharp
namespace Rymote.Konzole.Styling;

public enum ConsoleSegment
{
    Icon,
    Timestamp,
    Category,
    EventId,
    Scope,
    Message,
    MessageWarning,
    MessageError,
    LevelLabel,
    PropertyKey,
    PropertyValue,
    ExceptionLabel,
    ExceptionMessage,
    ExceptionStack
}
```

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

> **Design note for the implementer:** placing `LevelLabel` between the Message variants and Property segments keeps related visual chrome (icon, timestamp, category, eventId, scope, message, levelLabel) grouped before the property/exception details. Position in the enum doesn't matter for functionality — keys are looked up by name string at sink time.

---

### Task 2: Add `ShowIcon`, `ShowLevelLabel`, and default `LevelLabel` style to `ConsoleSinkOptions`

**Files:**
- Modify: `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs`

- [ ] **Step 1: Replace contents**

Replace `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs` with:

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Styling;

namespace Rymote.Konzole.Configuration;

public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    public bool UseEmojis { get; set; } = true;
    public bool ShowIcon { get; set; } = true;
    public bool ShowLevelLabel { get; set; } = true;

    public IReadOnlyDictionary<LogLevel, AnsiColor> LevelColors { get; init; } = new Dictionary<LogLevel, AnsiColor>
    {
        [LogLevel.Trace]       = AnsiColor.Named(AnsiNamedColor.BrightBlack),
        [LogLevel.Debug]       = AnsiColor.Named(AnsiNamedColor.White),
        [LogLevel.Information] = AnsiColor.Named(AnsiNamedColor.BrightCyan),
        [LogLevel.Warning]     = AnsiColor.Named(AnsiNamedColor.BrightYellow),
        [LogLevel.Error]       = AnsiColor.Named(AnsiNamedColor.BrightRed),
        [LogLevel.Critical]    = AnsiColor.Named(AnsiNamedColor.BrightWhite)
    };

    public IReadOnlyDictionary<KonzoleTag, AnsiColor> TagColors { get; init; } = new Dictionary<KonzoleTag, AnsiColor>
    {
        [KonzoleTag.Success]  = AnsiColor.Named(AnsiNamedColor.BrightGreen),
        [KonzoleTag.Pending]  = AnsiColor.Named(AnsiNamedColor.BrightBlue),
        [KonzoleTag.Complete] = AnsiColor.Named(AnsiNamedColor.Green),
        [KonzoleTag.Note]     = AnsiColor.Named(AnsiNamedColor.BrightMagenta),
        [KonzoleTag.Start]    = AnsiColor.Named(AnsiNamedColor.Cyan),
        [KonzoleTag.Pause]    = AnsiColor.Named(AnsiNamedColor.Yellow),
        [KonzoleTag.Watch]    = AnsiColor.Named(AnsiNamedColor.Magenta)
    };

    public AnsiColor CriticalBackgroundColor { get; init; } = AnsiColor.Named(AnsiNamedColor.Red);

    public IReadOnlyDictionary<ConsoleSegment, AnsiStyle> SegmentStyles { get; init; } = new Dictionary<ConsoleSegment, AnsiStyle>
    {
        [ConsoleSegment.Timestamp]        = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.Category]         = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) },
        [ConsoleSegment.EventId]          = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.Scope]            = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack), Decoration = AnsiTextDecoration.Italic },
        [ConsoleSegment.Message]          = AnsiStyle.Empty,
        [ConsoleSegment.MessageWarning]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightYellow) },
        [ConsoleSegment.MessageError]     = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightRed), Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.LevelLabel]       = new() { Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.PropertyKey]      = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.PropertyValue]    = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightWhite) },
        [ConsoleSegment.ExceptionLabel]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red), Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.ExceptionMessage] = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red) },
        [ConsoleSegment.ExceptionStack]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
    };
}
```

The change vs. current file is exactly three additions:
1. `public bool ShowIcon { get; set; } = true;`
2. `public bool ShowLevelLabel { get; set; } = true;`
3. `[ConsoleSegment.LevelLabel] = new() { Decoration = AnsiTextDecoration.Bold }` entry in `SegmentStyles`.

> **Design note for the implementer:** the `LevelLabel` default has no `Foreground` — `ConsoleSink` injects the dynamic level/tag color per-entry (same mechanism as the `Icon` segment). The bold decoration is preserved by `AnsiStyle.MergeWith`'s OR-of-flags semantics when the sink rebuilds the style.

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

## Phase 2 — Formatter + sink wiring (green throughout)

### Task 3: Rewrite `ConsoleFormatter` with new constructor + icon/label rendering (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/ConsoleFormatter.cs`
- Modify: `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs` (append 5 tests)

- [ ] **Step 1: Append the 5 new tests to `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs`**

Append to the existing class (do NOT replace the file):

```csharp
[Fact]
public void Format_WithShowLevelLabelTrue_EmitsLevelLabelSegment()
{
    ConsoleFormatter formatter = new(useEmojis: false, showIcon: false, showLevelLabel: true);
    LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.Contains("[level-label][INFO][/]", rendered);
}

[Fact]
public void Format_WithShowLevelLabelFalse_OmitsLevelLabelSegment()
{
    ConsoleFormatter formatter = new(useEmojis: false, showIcon: true, showLevelLabel: false);
    LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.DoesNotContain("[level-label]", rendered);
}

[Fact]
public void Format_WithShowIconFalse_OmitsIconSegment()
{
    ConsoleFormatter formatter = new(useEmojis: true, showIcon: false, showLevelLabel: true);
    LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.DoesNotContain("[icon]", rendered);
}

[Fact]
public void Format_LevelLabel_UsesTagText_WhenTagSet()
{
    ConsoleFormatter formatter = new(useEmojis: false, showIcon: false, showLevelLabel: true);
    LogEntry entry = new() { Level = LogLevel.Information, Tag = KonzoleTag.Success, Message = "x" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.Contains("[level-label][SUCCESS][/]", rendered);
    Assert.DoesNotContain("[INFO]", rendered);
}

[Fact]
public void Format_BothShowIconAndShowLevelLabelFalse_EmitsNeither()
{
    ConsoleFormatter formatter = new(useEmojis: true, showIcon: false, showLevelLabel: false);
    LogEntry entry = new() { Level = LogLevel.Information, Message = "x" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.DoesNotContain("[icon]", rendered);
    Assert.DoesNotContain("[level-label]", rendered);
}
```

- [ ] **Step 2: Run new tests — confirm 4 of them fail (one fails on Step 3 too — the level-label assertion has no chance until the formatter emits it)**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~ConsoleFormatterStyledTests
```

Expected: the new 5 tests will fail at compile or at assertion time because the formatter constructor doesn't accept `showIcon`/`showLevelLabel` yet.

- [ ] **Step 3: Replace `Rymote.Konzole/Formatters/ConsoleFormatter.cs`**

```csharp
using System.Text;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Formatters;

public sealed class ConsoleFormatter : ILogFormatter
{
    private readonly bool _useEmojis;
    private readonly bool _showIcon;
    private readonly bool _showLevelLabel;

    public ConsoleFormatter(bool useEmojis = true, bool showIcon = true, bool showLevelLabel = true)
    {
        _useEmojis = useEmojis;
        _showIcon = showIcon;
        _showLevelLabel = showLevelLabel;
    }

    public string Format(LogEntry entry, FormatterContext context)
    {
        StringBuilder stringBuilder = new();

        bool consoleSupportsUtf8 = Console.OutputEncoding.CodePage == 65001;
        bool emojiRenderable = _useEmojis && consoleSupportsUtf8;

        if (_showIcon)
        {
            if (emojiRenderable)
            {
                stringBuilder.Append("[icon]");
                stringBuilder.Append(entry.Tag.HasValue
                    ? LogIcon.GetIcon(entry.Tag.Value)
                    : LogIcon.GetIcon(entry.Level));
                stringBuilder.Append("[/]  ");
            }
            else if (!_showLevelLabel)
            {
                stringBuilder.Append("[icon]");
                stringBuilder.Append(entry.Tag.HasValue
                    ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                    : LogIcon.GetFallbackIcon(entry.Level));
                stringBuilder.Append("[/] ");
            }
        }

        if (_showLevelLabel)
        {
            string labelText = entry.Tag.HasValue
                ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                : LogIcon.GetFallbackIcon(entry.Level);
            stringBuilder.Append("[level-label]");
            stringBuilder.Append(labelText);
            stringBuilder.Append("[/] ");
        }

        AppendSegmentedTimestamp(stringBuilder, entry, context);
        AppendSegmentedCategory(stringBuilder, entry, context);
        AppendSegmentedEventId(stringBuilder, entry, context);
        AppendSegmentedScope(stringBuilder, entry, context);

        string messageSegment = entry.Level switch
        {
            LogLevel.Warning  => "message-warning",
            LogLevel.Error    => "message-error",
            LogLevel.Critical => "message-error",
            _                 => "message"
        };
        stringBuilder.Append('[').Append(messageSegment).Append(']');
        stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));
        stringBuilder.Append("[/]");

        if (entry.Properties is { Count: > 0 })
        {
            stringBuilder.Append(' ').Append('(');
            bool isFirst = true;
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                if (!isFirst) stringBuilder.Append(", ");
                stringBuilder.Append("[property-key]").Append(property.Key).Append("[/]");
                stringBuilder.Append(": ");
                stringBuilder.Append("[property-value]").Append(property.Value?.ToString() ?? "null").Append("[/]");
                isFirst = false;
            }
            stringBuilder.Append(')');
        }

        AppendSegmentedException(stringBuilder, entry, context);

        return stringBuilder.ToString();
    }

    private static void AppendSegmentedTimestamp(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowTimestamp) return;
        builder.Append("[timestamp][");
        builder.Append(entry.Timestamp.ToString(context.TimestampFormat));
        builder.Append("][/] ");
    }

    private static void AppendSegmentedCategory(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowCategory || string.IsNullOrEmpty(entry.Category)) return;
        builder.Append("[category][");
        builder.Append(TruncateCategory(entry.Category));
        builder.Append("][/] ");
    }

    private static void AppendSegmentedEventId(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowEventId || entry.EventId.Id == 0) return;
        builder.Append("[event-id][");
        builder.Append(entry.EventId.Id);
        if (!string.IsNullOrEmpty(entry.EventId.Name))
        {
            builder.Append(':');
            builder.Append(entry.EventId.Name);
        }
        builder.Append("][/] ");
    }

    private static void AppendSegmentedScope(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowScope || string.IsNullOrEmpty(entry.Scope)) return;
        builder.Append("[scope]=> ");
        builder.Append(entry.Scope);
        builder.Append("[/] ");
    }

    private static void AppendSegmentedException(StringBuilder builder, LogEntry entry, FormatterContext context)
    {
        if (!context.ShowException || entry.Exception == null) return;
        builder.AppendLine();
        builder.Append("    [exception-label]Exception:[/] ");
        builder.Append("[exception-message]").Append(entry.Exception.GetType().Name).Append("[/]");
        builder.AppendLine();
        builder.Append("    [exception-label]Message:[/] ");
        builder.Append("[exception-message]").Append(entry.Exception.Message).Append("[/]");
        if (string.IsNullOrEmpty(entry.Exception.StackTrace)) return;
        builder.AppendLine();
        builder.Append("    [exception-label]Stack Trace:[/]");
        foreach (string line in entry.Exception.StackTrace.Split('\n'))
        {
            builder.AppendLine();
            builder.Append("      [exception-stack]").Append(line.Trim()).Append("[/]");
        }
    }

    private static string TruncateCategory(string category)
    {
        const int maximumLength = 30;
        if (category.Length <= maximumLength) return category;

        string[] parts = category.Split('.');
        if (parts.Length == 1) return category.Substring(0, maximumLength - 3) + "...";

        StringBuilder builder = new();
        for (int index = 0; index < parts.Length - 1; index++)
        {
            builder.Append(parts[index][0]);
            builder.Append('.');
        }
        builder.Append(parts[^1]);

        return builder.Length > maximumLength
            ? builder.ToString(0, maximumLength - 3) + "..."
            : builder.ToString();
    }
}
```

The change vs. the current file:
1. Two new private fields `_showIcon`, `_showLevelLabel`
2. Constructor gains two default-true parameters
3. Icon rendering block now gated by `_showIcon`, with the bracket-fallback case ALSO gated by `!_showLevelLabel` (so the label doesn't duplicate)
4. New `[level-label][LABEL][/]` emission block after the icon, gated by `_showLevelLabel`

> **Design note for the implementer:** the label text from `LogIcon.GetFallbackIcon(entry.Level)` already returns e.g. `"[INFO]"` (with brackets). When emitted inside `[level-label]...[/]`, the inner brackets tokenize as an `OpenTag("INFO")` that doesn't match the palette OR the style grammar — so it falls through as literal `[INFO]` text. The `AnsiEmitter` already implements this leniency correctly; no parser changes needed.

- [ ] **Step 4: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~ConsoleFormatterStyledTests
```

Expected: all `ConsoleFormatterStyledTests` pass (11 total — 6 existing + 5 new).

- [ ] **Step 5: Run full suite to confirm no regressions**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj
```

Expected: all tests pass.

---

### Task 4: Update `ConsoleSink` — pass options to formatter, add `level-label` palette entry

**Files:**
- Modify: `Rymote.Konzole/Sinks/ConsoleSink.cs`
- Modify: `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs` (append 1 test)

- [ ] **Step 1: Append the new test to `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs`**

Append to the existing class (do NOT replace):

```csharp
[Fact]
public async Task EmitsAnsiEscapes_ForLevelLabel()
{
    StringWriter capturedStandardOut = new();
    TextWriter originalStandardOut = Console.Out;
    Console.SetOut(capturedStandardOut);

    try
    {
        ConsoleSinkOptions options = new()
        {
            UseColors = true,
            UseEmojis = false,
            ShowIcon = false,
            ShowLevelLabel = true,
            ShowTimestamp = false,
            ShowCategory = false
        };
        await using ConsoleSink sink = new(options);

        sink.TryEnqueue(new LogEntry { Level = LogLevel.Error, Message = "boom" });
        await sink.FlushAsync(CancellationToken.None);
    }
    finally
    {
        Console.SetOut(originalStandardOut);
    }

    string output = capturedStandardOut.ToString();
    Assert.Contains("\x1b[1m", output);
    Assert.Contains("[ERROR]", output);
    Assert.DoesNotContain("[level-label]", output);
}
```

- [ ] **Step 2: Replace `Rymote.Konzole/Sinks/ConsoleSink.cs`**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Rymote.Konzole.Styling;

namespace Rymote.Konzole.Sinks;

public sealed class ConsoleSink : SinkBase<ConsoleSinkOptions>
{
    private readonly Lock _consoleGate = new();
    private readonly IReadOnlyDictionary<string, AnsiStyle> _segmentPalette;

    public ConsoleSink(ConsoleSinkOptions options) : base(options)
    {
        _segmentPalette = BuildSegmentPalette(options.SegmentStyles);
    }

    public override string Name => "Console";

    protected override ILogFormatter CreateDefaultFormatter() =>
        new ConsoleFormatter(Options.UseEmojis, Options.ShowIcon, Options.ShowLevelLabel);

    protected override ValueTask WriteBatchAsync(IReadOnlyList<LogEntry> batch, CancellationToken cancellationToken)
    {
        foreach (LogEntry entry in batch)
        {
            string rendered = Formatter.Format(entry, FormatterContext);

            if (!Options.UseColors)
            {
                string plain = StyleMarkup.Strip(rendered);
                TextWriter plainDestination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;
                lock (_consoleGate) { plainDestination.WriteLine(plain); }
                continue;
            }

            AnsiOptions ansiOptions = new()
            {
                BaseStyle = AnsiStyle.Empty,
                SegmentPalette = ExtendPaletteWithDynamicSegments(entry)
            };
            string ansiOutput = StyleMarkup.ToAnsi(rendered, ansiOptions);

            TextWriter destination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;
            lock (_consoleGate)
            {
                destination.WriteLine(ansiOutput);
            }
        }

        return ValueTask.CompletedTask;
    }

    private IReadOnlyDictionary<string, AnsiStyle> ExtendPaletteWithDynamicSegments(LogEntry entry)
    {
        Dictionary<string, AnsiStyle> palette = new(_segmentPalette, StringComparer.OrdinalIgnoreCase);
        AnsiColor? dynamicColor = ResolveDynamicColor(entry);

        palette["icon"] = palette.TryGetValue("icon", out AnsiStyle iconBase)
            ? iconBase with { Foreground = dynamicColor }
            : new AnsiStyle { Foreground = dynamicColor };

        palette["level-label"] = palette.TryGetValue("level-label", out AnsiStyle labelBase)
            ? labelBase with { Foreground = dynamicColor }
            : new AnsiStyle { Foreground = dynamicColor, Decoration = AnsiTextDecoration.Bold };

        if (entry.Level == LogLevel.Critical)
        {
            AnsiStyle messageErrorStyle = palette.TryGetValue("message-error", out AnsiStyle existing) ? existing : AnsiStyle.Empty;
            palette["message-error"] = messageErrorStyle with { Background = Options.CriticalBackgroundColor };
        }

        return palette;
    }

    private AnsiColor? ResolveDynamicColor(LogEntry entry)
    {
        if (entry.Tag.HasValue && Options.TagColors.TryGetValue(entry.Tag.Value, out AnsiColor tagColor))
            return tagColor;

        return Options.LevelColors.TryGetValue(entry.Level, out AnsiColor levelColor) ? levelColor : null;
    }

    private static IReadOnlyDictionary<string, AnsiStyle> BuildSegmentPalette(IReadOnlyDictionary<ConsoleSegment, AnsiStyle> source)
    {
        Dictionary<string, AnsiStyle> palette = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<ConsoleSegment, AnsiStyle> entry in source)
        {
            palette[SegmentToMarkupKey(entry.Key)] = entry.Value;
        }
        return palette;
    }

    private static string SegmentToMarkupKey(ConsoleSegment segment) => segment switch
    {
        ConsoleSegment.Icon             => "icon",
        ConsoleSegment.Timestamp        => "timestamp",
        ConsoleSegment.Category         => "category",
        ConsoleSegment.EventId          => "event-id",
        ConsoleSegment.Scope            => "scope",
        ConsoleSegment.Message          => "message",
        ConsoleSegment.MessageWarning   => "message-warning",
        ConsoleSegment.MessageError     => "message-error",
        ConsoleSegment.LevelLabel       => "level-label",
        ConsoleSegment.PropertyKey      => "property-key",
        ConsoleSegment.PropertyValue    => "property-value",
        ConsoleSegment.ExceptionLabel   => "exception-label",
        ConsoleSegment.ExceptionMessage => "exception-message",
        ConsoleSegment.ExceptionStack   => "exception-stack",
        _                               => segment.ToString().ToLowerInvariant()
    };
}
```

The changes vs. current file:
1. `CreateDefaultFormatter` passes `Options.ShowIcon` and `Options.ShowLevelLabel` to the new constructor.
2. The old `ExtendPaletteWithIconAndCritical` method is renamed to `ExtendPaletteWithDynamicSegments` and now ALSO injects the dynamic foreground for `level-label`. It also preserves the existing palette's base style (decoration etc.) instead of overwriting it — a small improvement that means user `SegmentStyles[Icon] = { Decoration = Bold }` would now be respected too.
3. `ResolveIconColor` renamed to `ResolveDynamicColor` (same logic — picks tag color when set, else level color).
4. `SegmentToMarkupKey` gains the `ConsoleSegment.LevelLabel => "level-label"` arm.

> **Design note for the implementer:** the `with { Foreground = dynamicColor }` pattern preserves existing decoration flags from the user's palette override. If a user sets `SegmentStyles[LevelLabel] = new() { Decoration = Italic }`, the level label ends up `Italic` + dynamic foreground (no `Bold`, because the user explicitly opted out of the default bold).

- [ ] **Step 3: Build and run full suite**

```bash
dotnet build Rymote.Konzole.sln
dotnet test Rymote.Konzole.sln
```

Expected: 0 build errors. All tests pass (the previous 107 plus the 6 new ones from Tasks 3 + 4 = 113 total).

---

## Phase 3 — Version bump and verification

### Task 5: Bump version to 2.2.0 and final green check

**Files:**
- Modify: `Rymote.Konzole/Rymote.Konzole.csproj`

- [ ] **Step 1: Edit the version line**

In `Rymote.Konzole/Rymote.Konzole.csproj`, change:

```xml
<Version>2.1.0</Version>
```

to:

```xml
<Version>2.2.0</Version>
```

- [ ] **Step 2: Final build + test**

```bash
dotnet build Rymote.Konzole.sln
dotnet test Rymote.Konzole.sln
```

Expected: 0 errors, 0 unexpected warnings. All 113 tests pass.

- [ ] **Step 3: Owner reviews and commits**

NO COMMIT by the implementer — the owner reviews the diff and decides commit shape.

---

## Self-review

I checked the plan against the spec.

**1. Spec coverage**

| Spec section | Covered by |
|---|---|
| §3.1 `ShowIcon`, `ShowLevelLabel` options | Task 2 |
| §3.2 `ConsoleSegment.LevelLabel` enum value | Task 1 |
| §3.3 Default `LevelLabel` SegmentStyles | Task 2 |
| §3.4 `ConsoleFormatter` constructor change | Task 3 |
| §3.5 `ConsoleSink` palette wiring | Task 4 |
| §4 Output composition rules | Task 3 (formatter prefix-rendering block) |
| §5 Markup → ANSI flow | No new code — existing leniency in `AnsiEmitter` handles it |
| §6 Testing | Tasks 3 and 4 cover all 6 tests |
| §7 Versioning | Task 5 |

No gaps.

**2. Placeholder scan**

- No `TBD`, `TODO`, or "implement later" markers.
- Every code step shows complete code.
- No "Similar to Task N" without showing the actual code.

**3. Type / method consistency**

- `ConsoleFormatter` constructor signature: `(bool useEmojis = true, bool showIcon = true, bool showLevelLabel = true)` — consistent across Tasks 3 (definition) and 4 (`CreateDefaultFormatter` call site).
- `ConsoleSink.ExtendPaletteWithDynamicSegments` referenced once (in `WriteBatchAsync` — Task 4); the old `ExtendPaletteWithIconAndCritical` is fully replaced in the same task.
- `level-label` string key used consistently in `SegmentToMarkupKey` (Task 4), in the formatter emission (Task 3), in the palette injection (Task 4), and in the tests (Tasks 3 and 4).
- `ConsoleSegment.LevelLabel` referenced in Task 1 (declaration), Task 2 (SegmentStyles default), and Task 4 (SegmentToMarkupKey arm).

No drift.

---

## Execution Handoff

Plan saved at `docs/superpowers/plans/2026-05-21-level-label.md`. Proceeding to subagent-driven implementation immediately (owner already confirmed: small feature, no approval needed between plan and execution).
