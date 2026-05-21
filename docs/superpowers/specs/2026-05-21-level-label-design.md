# Level Label + Icon Toggle — Design

**Date:** 2026-05-21
**Status:** Approved for planning
**Target version:** `2.2.0` (additive feature; no breaking changes)

## 1. Goals

1. Add a bracketed level/tag label (`[INFO]`, `[ERROR]`, `[SUCCESS]`, etc.) as a distinct, always-available output segment in `ConsoleFormatter` — independent of whether the emoji icon renders.
2. Add a master toggle to disable the icon (emoji + bracket fallback) entirely.
3. Keep the change additive — no breaking signatures.

## 2. Non-goals

- Replacing the existing icon mechanism. Emojis remain the default leading element.
- Restructuring `ConsoleFormatter` or `ConsoleSinkOptions` beyond the new options + segment.
- Configurable label format (full names, lowercase, etc.). Uppercase abbreviated is the only supported format in this release.
- Per-level label text overrides. Users override the *style* via `SegmentStyles`, not the *text*.

## 3. Public surface

### 3.1 `ConsoleSinkOptions` additions

```csharp
public bool ShowIcon { get; set; } = true;
public bool ShowLevelLabel { get; set; } = true;
```

Both default `true` — the user said `We need` the labels, so they're on out of the box. Disable explicitly.

### 3.2 `ConsoleSegment` addition

```csharp
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
    LevelLabel,          // ← new
    PropertyKey,
    PropertyValue,
    ExceptionLabel,
    ExceptionMessage,
    ExceptionStack
}
```

### 3.3 `ConsoleSinkOptions.SegmentStyles` default for `LevelLabel`

```csharp
[ConsoleSegment.LevelLabel] = new() { Decoration = AnsiTextDecoration.Bold }
```

No foreground color in the default — `ConsoleSink` injects it dynamically per-entry (the level color when no tag, the tag color when a tag is set). The bold decoration is preserved by `AnsiStyle.MergeWith`'s OR-of-flags semantics.

### 3.4 `ConsoleFormatter` constructor

```csharp
public ConsoleFormatter(bool useEmojis = true, bool showIcon = true, bool showLevelLabel = true)
```

The two new parameters have defaults so existing call sites (`new ConsoleFormatter(useEmojis: false)`, etc.) keep compiling.

`ConsoleSink.CreateDefaultFormatter()` becomes:
```csharp
protected override ILogFormatter CreateDefaultFormatter() =>
    new ConsoleFormatter(Options.UseEmojis, Options.ShowIcon, Options.ShowLevelLabel);
```

### 3.5 `ConsoleSink` palette wiring

`ExtendPaletteWithIconAndCritical` gains a sibling injection for `level-label`:

```csharp
private IReadOnlyDictionary<string, AnsiStyle> ExtendPaletteWithIconAndCritical(LogEntry entry)
{
    Dictionary<string, AnsiStyle> palette = new(_segmentPalette, StringComparer.OrdinalIgnoreCase);

    AnsiColor? dynamicColor = ResolveIconColor(entry);

    palette["icon"] = palette.TryGetValue("icon", out AnsiStyle iconBase)
        ? iconBase with { Foreground = dynamicColor }
        : new AnsiStyle { Foreground = dynamicColor };

    palette["level-label"] = palette.TryGetValue("level-label", out AnsiStyle labelBase)
        ? labelBase with { Foreground = dynamicColor }
        : new AnsiStyle { Foreground = dynamicColor, Decoration = AnsiTextDecoration.Bold };

    if (entry.Level == LogLevel.Critical)
    {
        AnsiStyle messageErrorStyle = palette.TryGetValue("message-error", out AnsiStyle existing)
            ? existing
            : AnsiStyle.Empty;
        palette["message-error"] = messageErrorStyle with { Background = Options.CriticalBackgroundColor };
    }

    return palette;
}
```

`SegmentToMarkupKey` gains the `LevelLabel → "level-label"` entry.

## 4. Output composition

`ConsoleFormatter.Format` rendering of the prefix becomes (pseudocode):

```
bool emojiRenderable = useEmojis && Console.OutputEncoding.CodePage == 65001;

if (showIcon)
{
    if (emojiRenderable)
    {
        append "[icon]" + emoji + "[/]  "
    }
    else if (!showLevelLabel)
    {
        append "[icon]" + fallbackText + "[/] "   // existing bracket fallback only when label is off
    }
    // else: label carries the marker; icon emits nothing
}

if (showLevelLabel)
{
    string labelText = entry.Tag.HasValue
        ? LogIcon.GetFallbackIcon(entry.Tag.Value)   // "[SUCCESS]"
        : LogIcon.GetFallbackIcon(entry.Level);       // "[INFO]"
    append "[level-label]" + labelText + "[/] "
}

// timestamp, category, eventId, scope, message, properties, exception ...
```

The full prefix matrix:

| `ShowIcon` | `ShowLevelLabel` | UTF-8 | Prefix |
|------------|------------------|-------|--------|
| true | true | yes | `ℹ️  [INFO] ` |
| true | true | no  | `[INFO] ` |
| true | false | yes | `ℹ️  ` |
| true | false | no  | `[INFO] ` (existing bracket-fallback preserved) |
| false | true | any | `[INFO] ` |
| false | false | any | _(empty — line starts at the next segment)_ |

## 5. Markup → ANSI flow

The formatter emits markup like:
```
[icon]ℹ️[/]  [level-label][INFO][/] [timestamp][2026-05-21 12:00:00.123][/] ...
```

The token sequence the parser produces for `[level-label][INFO][/]`:
- `OpenTag("level-label")` — palette lookup succeeds → push merged style
- `OpenTag("INFO")` — palette miss, grammar miss → emit literal text `[INFO]`
- `CloseTag` — pop, restore outer style

Result: `[INFO]` text rendered with the level-label style (dynamic foreground + bold).

This relies on the existing leniency rule and requires no parser/emitter changes.

## 6. Testing

Five new tests in `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs`:

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

One ConsoleSink integration test in `ConsoleSinkTests.cs`:

```csharp
[Fact]
public async Task EmitsAnsiEscapes_ForLevelLabel()
{
    StringWriter captured = new();
    TextWriter original = Console.Out;
    Console.SetOut(captured);

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
        Console.SetOut(original);
    }

    string output = captured.ToString();
    Assert.Contains("\x1b[1m", output);      // bold from the LevelLabel decoration
    Assert.Contains("[ERROR]", output);       // the literal text passed through
    Assert.DoesNotContain("[level-label]", output); // markup tag converted, not literal
}
```

## 7. Versioning

This is an additive minor release. Existing source compiles unchanged:
- New `ConsoleSinkOptions.ShowIcon` and `ShowLevelLabel` default to `true` — the rendered output gains the bracketed label, but no code change is required to opt out.
- New `ConsoleSegment.LevelLabel` enum value — appended; existing switch statements over `ConsoleSegment` would emit a CS8524 if they don't handle it, but no consumer outside Konzole switches over this enum.
- `ConsoleFormatter` constructor gains two default-true parameters — existing calls keep working.

**Target version: 2.2.0** (minor bump from 2.1.0).

## 8. Open questions

None. All design decisions resolved during brainstorming.
