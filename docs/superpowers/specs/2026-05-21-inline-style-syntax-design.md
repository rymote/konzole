# Inline Style Syntax — Design

**Date:** 2026-05-21
**Status:** Approved for planning
**Target version:** `2.1.0` (additive feature; no breaking changes)

## 1. Goals

1. Let users embed inline style markup in log message strings — colors, backgrounds, bold/italic/underline/dim/reverse — using a BBCode-style `[tag]content[/]` grammar.
2. Apply that markup as ANSI escape sequences in `ConsoleSink`; strip it cleanly in every other sink (File / Remote / Discord / Slack) so non-terminal consumers receive plain text.
3. Make the default `ConsoleSink` output multi-colored per-segment (timestamp dim, category cyan, message in level color for errors, etc.) — the line is more scannable without any user configuration.
4. Keep the surface area additive — no breaking changes to v2.0.x.

## 2. Non-goals

- Replacing the ConsoleColor-based level/tag color maps wholesale. (They become `AnsiColor` internally, but the existing `ConsoleColor` defaults convert implicitly so user-facing API stays compatible.)
- A general templating language. The grammar covers styling tags only — no logic, conditionals, or substitution.
- Auto-detection of terminal capabilities. We emit ANSI unconditionally on .NET 10+ where Windows VT processing is on by default; users on legacy terminals get raw escape codes (rare in 2026).
- Color theming presets. Defaults are tuned for a dark terminal; per-segment overrides cover light terminals.

## 3. Grammar

### 3.1 Token syntax

```
[<style-list>]content[/]
[[              → literal '['
```

`<style-list>` is one or more whitespace-separated tokens. Token names are case-insensitive.

| Category   | Tokens |
|------------|--------|
| Foreground (named, 16) | `black`, `red`, `green`, `yellow`, `blue`, `magenta`, `cyan`, `white`, `bright_black`, `bright_red`, `bright_green`, `bright_yellow`, `bright_blue`, `bright_magenta`, `bright_cyan`, `bright_white` |
| Background      | `bg:<any foreground named token>` (e.g. `bg:yellow`, `bg:bright_red`) |
| 256-color palette | `color:N` (foreground, N=0-255), `bg:color:N` (background) |
| Truecolor hex     | `#rrggbb` (foreground), `bg:#rrggbb` (background) |
| Text decoration   | `bold`, `italic`, `underline`, `dim`, `reverse` |

Combinations are free-form, whitespace-separated inside a single tag:
`[bold bright_red bg:black]critical[/]`, `[italic #7c3aed underline]heads up[/]`.

### 3.2 Open/close semantics

- `[/]` closes the most recently opened tag (LIFO stack).
- Nested tags compose: foreground/background of the inner layer override the outer; decoration flags OR together.
- Unclosed tags are implicitly closed at end-of-string (an `\x1b[0m` reset is appended).
- A bare `[/]` with nothing open is silently dropped (no error, no output).

### 3.3 Leniency

- Any `[xxx]` whose body doesn't parse as a valid style list passes through as literal text. So `[Login]`, `[2026-05-21]`, `[App.Service]`, the formatter's own timestamp/category brackets all render unaffected.
- `[[` → literal `[`. Use when the user wants the literal string `[red]` rendered without styling.

### 3.4 Examples

| Input | Console output (conceptually) |
|-------|-------------------------------|
| `User [bold]Alice[/] logged in` | "User", **Alice**, " logged in" |
| `[red]error: [bold]CRITICAL[/] details[/]` | full string in red, `CRITICAL` also bold |
| `[bg:yellow black]heads up[/]` | "heads up" in black on yellow |
| `[#ff8800]warn[/]` | "warn" in orange truecolor |
| `[[red] is the color word` | literal `[red] is the color word` |
| `[Login] complete` | literal `[Login] complete` (lenient — unknown tag) |

## 4. Architecture

### 4.1 Files added under `Rymote.Konzole/Styling/`

```
Styling/
├── StyleMarkup.cs              (public facade)
├── AnsiOptions.cs              (public)
├── AnsiStyle.cs                (public record struct)
├── AnsiColor.cs                (public record struct)
├── AnsiColorKind.cs            (public enum)
├── AnsiNamedColor.cs           (public enum)
├── AnsiTextDecoration.cs       (public flags enum)
├── ConsoleSegment.cs           (public enum)
├── StyleParser.cs              (internal)
├── StyleToken.cs               (internal record struct)
├── StyleTokenKind.cs           (internal enum)
├── AnsiEmitter.cs              (internal)
├── AnsiEmitContext.cs          (internal)
└── StripEmitter.cs             (internal)
```

### 4.2 Public surface

```csharp
public static class StyleMarkup
{
    public static string ToAnsi(string input);
    public static string ToAnsi(string input, AnsiOptions options);
    public static string Strip(string input);
}

public sealed class AnsiOptions
{
    public AnsiStyle? BaseStyle { get; init; }
    public IReadOnlyDictionary<string, AnsiStyle>? SegmentPalette { get; init; }
    public bool AppendFinalReset { get; init; } = true;
}

public readonly record struct AnsiStyle
{
    public AnsiColor? Foreground { get; init; }
    public AnsiColor? Background { get; init; }
    public AnsiTextDecoration Decoration { get; init; }
    public static AnsiStyle Empty => default;
}

public readonly record struct AnsiColor
{
    public AnsiColorKind Kind { get; init; }
    public byte Red { get; init; }            // valid when Kind == Truecolor
    public byte Green { get; init; }          // valid when Kind == Truecolor
    public byte Blue { get; init; }           // valid when Kind == Truecolor
    public byte PaletteIndex { get; init; }   // valid when Kind == Palette
    public AnsiNamedColor NamedColor { get; init; }   // valid when Kind == Named

    public static AnsiColor Named(AnsiNamedColor name) => new() { Kind = AnsiColorKind.Named, NamedColor = name };
    public static AnsiColor Palette(byte index) => new() { Kind = AnsiColorKind.Palette, PaletteIndex = index };
    public static AnsiColor Truecolor(byte red, byte green, byte blue) =>
        new() { Kind = AnsiColorKind.Truecolor, Red = red, Green = green, Blue = blue };
    public static implicit operator AnsiColor(ConsoleColor consoleColor) => /* see 4.5 */;
}
```

Fields not relevant to the current `Kind` are zero/default and ignored by the emitter. Construct only via the static factory methods to avoid invalid states.

```csharp

public enum AnsiColorKind { Named, Palette, Truecolor }

public enum AnsiNamedColor
{
    Black, Red, Green, Yellow, Blue, Magenta, Cyan, White,
    BrightBlack, BrightRed, BrightGreen, BrightYellow,
    BrightBlue, BrightMagenta, BrightCyan, BrightWhite
}

[Flags]
public enum AnsiTextDecoration { None = 0, Bold = 1, Italic = 2, Underline = 4, Dim = 8, Reverse = 16 }

public enum ConsoleSegment
{
    Icon, Timestamp, Category, EventId, Scope, Message,
    PropertyKey, PropertyValue,
    ExceptionLabel, ExceptionMessage, ExceptionStack
}
```

### 4.3 Parser / emitter pipeline

```
input string
    │
    ▼
StyleParser.Tokenize(input) → IReadOnlyList<StyleToken>
    │
    ▼
   ┌─────────────────────────┬─────────────────────────┐
   │                         │                         │
   ▼                         ▼                         ▼
ANSI emitter           Strip emitter            (future emitters)
(AnsiEmitter.Emit)     (StripEmitter.Emit)
```

`StyleParser` is the single source of truth for "what is a tag". It tokenizes left-to-right:

1. `[[` → emit `Text("[")`, advance 2.
2. `[/]` → emit `CloseTag`, advance 3.
3. `[` followed by content terminated by `]` and no nested `[` → emit `OpenTag(content)`. The parser does not validate the content here; downstream emitters decide whether to honor it.
4. Otherwise → accumulate into a `Text` run.

`AnsiEmitter` walks tokens with a `Stack<AnsiStyle>`:

1. Push `options.BaseStyle ?? AnsiStyle.Empty` as the bottom.
2. Emit ANSI for the base style at start (if non-empty).
3. `Text(s)` → append `s`.
4. `OpenTag(body)` → resolve via segment palette first (case-insensitive lookup), then style grammar. On success: compute effective style = top merged with new, push, emit reset + new effective. On failure (neither lookup nor grammar parses): treat as literal — append `"[" + body + "]"`.
5. `CloseTag` → if stack depth > 1: pop, emit reset + new top style. Otherwise drop.
6. End of input: if `AppendFinalReset`, append `\x1b[0m`.

`StripEmitter` walks tokens without a stack:
1. `Text(s)` → append.
2. `OpenTag(body)` → if `body` resolves as a valid style list (no segment palette here), drop. Otherwise append `"[" + body + "]"` as literal.
3. `CloseTag` → drop.

### 4.4 Delta encoding

Style transitions emit `\x1b[0m` followed by a full encoding of the new effective style. Slightly chattier than computing minimal SGR deltas but trivially correct, easier to test, and the overhead is invisible for a logger.

### 4.5 ConsoleColor → AnsiColor implicit conversion

```
ConsoleColor.Black        → AnsiColor.Named(AnsiNamedColor.Black)
ConsoleColor.DarkRed      → AnsiColor.Named(AnsiNamedColor.Red)
ConsoleColor.DarkGreen    → AnsiColor.Named(AnsiNamedColor.Green)
ConsoleColor.DarkYellow   → AnsiColor.Named(AnsiNamedColor.Yellow)
ConsoleColor.DarkBlue     → AnsiColor.Named(AnsiNamedColor.Blue)
ConsoleColor.DarkMagenta  → AnsiColor.Named(AnsiNamedColor.Magenta)
ConsoleColor.DarkCyan     → AnsiColor.Named(AnsiNamedColor.Cyan)
ConsoleColor.Gray         → AnsiColor.Named(AnsiNamedColor.White)
ConsoleColor.DarkGray     → AnsiColor.Named(AnsiNamedColor.BrightBlack)
ConsoleColor.Red          → AnsiColor.Named(AnsiNamedColor.BrightRed)
ConsoleColor.Green        → AnsiColor.Named(AnsiNamedColor.BrightGreen)
ConsoleColor.Yellow       → AnsiColor.Named(AnsiNamedColor.BrightYellow)
ConsoleColor.Blue         → AnsiColor.Named(AnsiNamedColor.BrightBlue)
ConsoleColor.Magenta      → AnsiColor.Named(AnsiNamedColor.BrightMagenta)
ConsoleColor.Cyan         → AnsiColor.Named(AnsiNamedColor.BrightCyan)
ConsoleColor.White        → AnsiColor.Named(AnsiNamedColor.BrightWhite)
```

This is what existing `ConsoleColor` keys in `LevelColors` / `TagColors` translate to when the dictionaries are typed as `IReadOnlyDictionary<…, AnsiColor>`. Source-level API for users that set `options.LevelColors[LogLevel.Information] = ConsoleColor.Cyan;` keeps compiling.

## 5. ConsoleSink and ConsoleFormatter changes

### 5.1 Default per-segment palette

Tuned for a dark terminal. All defaults baked into `ConsoleSinkOptions.SegmentStyles`.

| Segment             | Default `AnsiStyle` |
|---------------------|---------------------|
| `Icon`              | (level color or tag color, dynamic per entry — applied as base style) |
| `Timestamp`         | foreground `BrightBlack` |
| `Category`          | foreground `Cyan` |
| `EventId`           | foreground `BrightBlack` |
| `Scope`             | foreground `BrightBlack`, decoration `Italic` |
| `Message`           | empty for Trace/Debug/Information; foreground = level color for Warning+; decoration `Bold` adds for Error/Critical |
| `PropertyKey`       | foreground `BrightBlack` |
| `PropertyValue`     | foreground `BrightWhite` |
| `ExceptionLabel`    | foreground `Red`, decoration `Bold` |
| `ExceptionMessage`  | foreground `Red` |
| `ExceptionStack`    | foreground `BrightBlack` |

The Message-level logic (different style for different levels) lives in `ConsoleFormatter`, which picks the segment NAME to emit based on level: `message`, `message-warning`, `message-error`. The `ConsoleSinkOptions.SegmentStyles` map carries entries for all three so users can override per-level emphasis.

### 5.2 ConsoleSinkOptions additions

```csharp
public IReadOnlyDictionary<ConsoleSegment, AnsiStyle> SegmentStyles { get; init; }
```

Default value: the table above, frozen into a `Dictionary<ConsoleSegment, AnsiStyle>`. When the user provides their own dictionary, the implementation merges user entries on top of defaults (missing keys fall back to defaults).

### 5.3 ConsoleFormatter — new responsibilities

The formatter no longer outputs plain text. It outputs the same logical content wrapped in segment markup tags:

```
[icon]ℹ️[/]  [timestamp][2026-05-21 12:00:00.123][/] [category][App.Service][/] [scope]=> Login[/] [message]User Alice logged in[/] [property-key]userId[/]: [property-value]42[/]
```

The inner `[2026-05-21 12:00:00.123]` and `[App.Service]` brackets are passed through as literal text by leniency rule 3.3 (date strings and dotted names don't parse as valid style lists). The parser does not need a nested-content-scope concept; it simply tokenizes left-to-right and any `[...]` whose body isn't a valid style list or segment-palette key falls through as literal text. This means the formatter can safely place the existing bracketed timestamps/categories inside segment wrappers without escaping.

### 5.4 ConsoleSink — simplified

```csharp
protected override ValueTask WriteBatchAsync(...)
{
    foreach (LogEntry entry in batch)
    {
        string rendered = Formatter.Format(entry, FormatterContext);
        AnsiOptions options = BuildAnsiOptions(entry);
        string ansiOutput = StyleMarkup.ToAnsi(rendered, options);
        TextWriter destination = entry.Level >= LogLevel.Error ? Console.Error : Console.Out;
        lock (_consoleGate)
        {
            destination.WriteLine(ansiOutput);
        }
    }
    return ValueTask.CompletedTask;
}

private AnsiOptions BuildAnsiOptions(LogEntry entry)
{
    Dictionary<string, AnsiStyle> palette = new(StringComparer.OrdinalIgnoreCase);
    foreach (var kvp in Options.SegmentStyles)
        palette[SegmentToToken(kvp.Key, entry)] = kvp.Value;

    AnsiStyle? baseStyle = ResolveBaseStyle(entry);   // Icon segment style, derived from tag/level
    return new AnsiOptions { BaseStyle = baseStyle, SegmentPalette = palette };
}
```

No more `Console.ForegroundColor` / `Console.BackgroundColor` mutations. No try/finally restoring colors. Single `WriteLine` per entry. The `_consoleGate` lock stays — multiple sinks writing concurrently to stdout still need serialization.

The `ConsoleSegment` enum is extended to include level-emphasis variants for the message:

```csharp
public enum ConsoleSegment
{
    Icon, Timestamp, Category, EventId, Scope,
    Message, MessageWarning, MessageError,
    PropertyKey, PropertyValue,
    ExceptionLabel, ExceptionMessage, ExceptionStack
}
```

`ConsoleFormatter` picks which variant to wrap the message in based on `entry.Level`:
- `LogLevel.Trace`, `Debug`, `Information` → `[message]…[/]`
- `LogLevel.Warning` → `[message-warning]…[/]`
- `LogLevel.Error`, `Critical` → `[message-error]…[/]`

The `ConsoleSink` builds the segment palette dictionary using a fixed enum-to-string mapping: `ConsoleSegment.Message` → `"message"`, `MessageWarning` → `"message-warning"`, `MessageError` → `"message-error"`, all other segments use their lowercase enum name. Users override any segment via `ConsoleSinkOptions.SegmentStyles` and the override flows through the same conversion.

## 6. Non-console sinks

### 6.1 Where stripping happens

Each non-console formatter calls `StyleMarkup.Strip` on user-supplied text fields BEFORE embedding them in its own output. `FormatterHelpers.StripStyles(string)` is a thin wrapper for organizational consistency:

```csharp
internal static class FormatterHelpers
{
    public static string StripStyles(string text) => StyleMarkup.Strip(text);
    // ... existing helpers ...
}
```

### 6.2 Per-formatter changes

| Formatter | Fields to strip |
|-----------|-----------------|
| `JsonFormatter` | `entry.Message`, `entry.Exception?.Message`, each string property value, `entry.Scope` |
| `DiscordFormatter` | `entry.Message`, `entry.Category` if shown |
| `SlackFormatter` | `entry.Message`, `entry.Category` if shown |

Property values flowing through `JsonFormatter` may be non-strings — `StyleMarkup.Strip` only applies when the value is a string. The existing `SafeObjectJsonConverter` handles other types unchanged.

`DiscordFormatter` and `SlackFormatter` strip before composing markdown so the resulting Discord/Slack payload contains no `[red]` literals.

## 7. Testing

Test files added (all in `Rymote.Konzole.Tests/`):

```
Styling/
├── StyleParserTests.cs
├── AnsiEmitterTests.cs
├── StripEmitterTests.cs
└── StyleMarkupTests.cs

Formatters/
├── ConsoleFormatterStyledTests.cs (new)
├── JsonFormatterTests.cs (new test cases for stripping)
├── DiscordFormatterTests.cs (new test cases for stripping)
└── SlackFormatterTests.cs (new test cases for stripping)

Sinks/
└── ConsoleSinkTests.cs (new test cases for ANSI output)
```

### 7.1 Parser

- `[[red]` → `Text("[")`, `Text("red]")` — the `[[` escape turns into a literal `[`, then `red]` is a normal text run (since the next `[` would be needed to open a tag). Rendered: `[red]`.
- `[[red]]` → `Text("[")`, `Text("red]]")` — same escape, with an extra trailing `]`. Rendered: `[red]]`.
- `[2026-05-21] message` → no tag detected (digits/dashes not valid in tag names) — literal pass-through
- `[App.Service]` → literal pass-through (dots not valid)
- `[red][/]` → `OpenTag("red")`, `CloseTag`
- `[red][bold]x[/][/]` → nested tags, two close tags
- `[/]` alone → bare `CloseTag` (emitter drops)
- `[red` unterminated → `Text("[red")`
- `[red] unclosed at end` → `OpenTag("red")` + `Text("...")` (ANSI emitter resets at end)
- `[bg:yellow black]` → `OpenTag("bg:yellow black")` (single tag with two style tokens)
- `[bg:#ff8800]` → `OpenTag("bg:#ff8800")`
- `[BOLD RED]` → resolves case-insensitively to bold + red

### 7.2 ANSI emitter — golden output

Each test pins the exact ANSI byte sequence:

```csharp
[Fact] public void Red_WrapsTextWithSGR31_AndReset()
{
    Assert.Equal("\x1b[31merror\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[red]error[/]"));
}

[Fact] public void BoldRed_CombinesSGRCodes()
{
    Assert.Equal("\x1b[1;31mcritical\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[bold red]critical[/]"));
}

[Fact] public void Truecolor_UsesSGR38_2()
{
    Assert.Equal("\x1b[38;2;255;136;0mwarn\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[#ff8800]warn[/]"));
}

[Fact] public void Color256_UsesSGR38_5()
{
    Assert.Equal("\x1b[38;5;208mwarn\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[color:208]warn[/]"));
}

[Fact] public void Background_UsesSGR48()
{
    Assert.Equal("\x1b[43mwarn\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[bg:yellow]warn[/]"));
}

[Fact] public void BaseStyle_Applied_RestoredAfterClose()
{
    AnsiOptions options = new()
    {
        BaseStyle = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) }
    };
    string result = StyleMarkup.ToAnsi("hello [bold]you[/] there", options);
    Assert.StartsWith("\x1b[36m", result);
    Assert.Contains("\x1b[36;1m", result);  // cyan + bold combined
    Assert.EndsWith("\x1b[0m", result);
}
```

### 7.3 Strip emitter

```csharp
[Fact] public void Strip_RemovesTags()
    => Assert.Equal("error", StyleMarkup.Strip("[red]error[/]"));

[Fact] public void Strip_KeepsLiteralBrackets()
    => Assert.Equal("[2026-05-21] message", StyleMarkup.Strip("[2026-05-21] message"));

[Fact] public void Strip_ResolvesDoubleEscape()
    => Assert.Equal("[red]", StyleMarkup.Strip("[[red]"));

[Fact] public void Strip_DropsBareCloseTag()
    => Assert.Equal("text", StyleMarkup.Strip("text[/]"));
```

### 7.4 Formatter / sink integration

```csharp
[Fact] public void ConsoleFormatter_WrapsTimestampInTimestampSegment() { ... }
[Fact] public void ConsoleFormatter_ErrorMessage_WrappedInMessageErrorSegment() { ... }
[Fact] public async Task ConsoleSink_EmitsAnsiEscapes_ForLevelColor() { ... }
[Fact] public async Task ConsoleSink_EmitsAnsiEscapes_ForInlineUserMarkup() { ... }
[Fact] public void JsonFormatter_StripsStyleMarkup_FromMessage() { ... }
[Fact] public void DiscordFormatter_StripsStyleMarkup_FromMessage() { ... }
[Fact] public void SlackFormatter_StripsStyleMarkup_FromMessage() { ... }
```

## 8. Versioning

This is an additive feature with no breaking changes:
- All new types are new files / new public API.
- `ConsoleSinkOptions.SegmentStyles` is a new init-only property with defaults baked in.
- `ConsoleSinkOptions.LevelColors` / `TagColors` change type from `IReadOnlyDictionary<…, ConsoleColor>` to `IReadOnlyDictionary<…, AnsiColor>` — but the implicit `ConsoleColor → AnsiColor` conversion keeps source-compatible.
- `ConsoleSink` no longer mutates `Console.ForegroundColor` — observable behavior change for anyone reading those values mid-write (extremely unlikely).
- `JsonFormatter`/`DiscordFormatter`/`SlackFormatter` now strip style markup from message bodies — observable only if users were embedding bracket-tag-like strings in messages and expecting them to render literally on those sinks. Almost certainly not happening.

**Target version: 2.1.0** (minor bump).

## 9. Open questions

None. All design questions resolved during brainstorming.
