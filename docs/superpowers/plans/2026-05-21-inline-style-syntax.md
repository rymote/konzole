# Inline Style Syntax Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Commit policy:** This project's owner has a global rule against autonomous commits. Every step labelled "Commit" must be explicitly approved by the user before running the `git` command. If unsure, ask first.

**Goal:** Add inline style syntax (`[red]text[/]`, `[bold]`, `[#ff8800]`, etc.) to log messages — applied as ANSI escapes in `ConsoleSink`, stripped cleanly in every other sink. Default console output becomes multi-colored per segment for readability.

**Architecture:** Three new public types (`StyleMarkup`, `AnsiStyle`, `AnsiOptions`) expose a `[tag]content[/]` grammar via a pure transformation pipeline (`StyleParser` → `AnsiEmitter` or `StripEmitter`). `ConsoleSink` builds a per-entry segment palette from `ConsoleSinkOptions`, passes the formatter's segment-wrapped markup through `StyleMarkup.ToAnsi`, and writes the result. Non-console formatters call `FormatterHelpers.StripStyles` on user-supplied text fields before embedding.

**Tech Stack:** .NET 10, C#, xUnit. Pure string-processing — no new dependencies.

**Spec:** `docs/superpowers/specs/2026-05-21-inline-style-syntax-design.md`

---

## File map

### Created
- `Rymote.Konzole/Styling/StyleMarkup.cs`
- `Rymote.Konzole/Styling/AnsiOptions.cs`
- `Rymote.Konzole/Styling/AnsiStyle.cs`
- `Rymote.Konzole/Styling/AnsiColor.cs`
- `Rymote.Konzole/Styling/AnsiColorKind.cs`
- `Rymote.Konzole/Styling/AnsiNamedColor.cs`
- `Rymote.Konzole/Styling/AnsiTextDecoration.cs`
- `Rymote.Konzole/Styling/ConsoleSegment.cs`
- `Rymote.Konzole/Styling/StyleParser.cs`
- `Rymote.Konzole/Styling/StyleToken.cs`
- `Rymote.Konzole/Styling/StyleTokenKind.cs`
- `Rymote.Konzole/Styling/AnsiEmitter.cs`
- `Rymote.Konzole/Styling/AnsiEmitContext.cs`
- `Rymote.Konzole/Styling/StripEmitter.cs`
- `Rymote.Konzole.Tests/Styling/AnsiColorTests.cs`
- `Rymote.Konzole.Tests/Styling/StyleParserTests.cs`
- `Rymote.Konzole.Tests/Styling/AnsiEmitterTests.cs`
- `Rymote.Konzole.Tests/Styling/StripEmitterTests.cs`
- `Rymote.Konzole.Tests/Styling/StyleMarkupTests.cs`
- `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs`

### Modified
- `Rymote.Konzole/Formatters/FormatterHelpers.cs` (add `StripStyles` helper)
- `Rymote.Konzole/Formatters/JsonFormatter.cs` (strip on user-content fields)
- `Rymote.Konzole/Formatters/DiscordFormatter.cs` (strip on Message + Category)
- `Rymote.Konzole/Formatters/SlackFormatter.cs` (strip on Message + Category)
- `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs` (LevelColors/TagColors → AnsiColor, add SegmentStyles)
- `Rymote.Konzole/Formatters/ConsoleFormatter.cs` (emit segment-wrapped markup)
- `Rymote.Konzole/Sinks/ConsoleSink.cs` (build palette, call `StyleMarkup.ToAnsi`, no `Console.ForegroundColor` mutation)
- `Rymote.Konzole.Tests/Formatters/JsonFormatterTests.cs` (add strip test)
- `Rymote.Konzole.Tests/Formatters/DiscordFormatterTests.cs` (add strip test)
- `Rymote.Konzole.Tests/Formatters/SlackFormatterTests.cs` (add strip test)
- `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs` (add ANSI emission tests)
- `Rymote.Konzole/Rymote.Konzole.csproj` (bump `<Version>` to `2.1.0`)

### Deleted
- None.

---

## Phase 1 — Color & style primitives (additive, green throughout)

### Task 1: Enums and `AnsiOptions` shell

**Files:**
- Create: `Rymote.Konzole/Styling/AnsiColorKind.cs`
- Create: `Rymote.Konzole/Styling/AnsiNamedColor.cs`
- Create: `Rymote.Konzole/Styling/AnsiTextDecoration.cs`
- Create: `Rymote.Konzole/Styling/ConsoleSegment.cs`

- [ ] **Step 1: Create `AnsiColorKind.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public enum AnsiColorKind
{
    Named,
    Palette,
    Truecolor
}
```

- [ ] **Step 2: Create `AnsiNamedColor.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public enum AnsiNamedColor
{
    Black,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    White,
    BrightBlack,
    BrightRed,
    BrightGreen,
    BrightYellow,
    BrightBlue,
    BrightMagenta,
    BrightCyan,
    BrightWhite
}
```

- [ ] **Step 3: Create `AnsiTextDecoration.cs`**

```csharp
namespace Rymote.Konzole.Styling;

[Flags]
public enum AnsiTextDecoration
{
    None      = 0,
    Bold      = 1 << 0,
    Italic    = 1 << 1,
    Underline = 1 << 2,
    Dim       = 1 << 3,
    Reverse   = 1 << 4
}
```

- [ ] **Step 4: Create `ConsoleSegment.cs`**

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
    PropertyKey,
    PropertyValue,
    ExceptionLabel,
    ExceptionMessage,
    ExceptionStack
}
```

- [ ] **Step 5: Build to confirm**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

### Task 2: `AnsiColor` record struct (TDD)

**Files:**
- Create: `Rymote.Konzole/Styling/AnsiColor.cs`
- Create: `Rymote.Konzole.Tests/Styling/AnsiColorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `Rymote.Konzole.Tests/Styling/AnsiColorTests.cs`:

```csharp
using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class AnsiColorTests
{
    [Fact]
    public void Named_ProducesNamedKind()
    {
        AnsiColor color = AnsiColor.Named(AnsiNamedColor.Red);
        Assert.Equal(AnsiColorKind.Named, color.Kind);
        Assert.Equal(AnsiNamedColor.Red, color.NamedColor);
    }

    [Fact]
    public void Palette_ProducesPaletteKind()
    {
        AnsiColor color = AnsiColor.Palette(208);
        Assert.Equal(AnsiColorKind.Palette, color.Kind);
        Assert.Equal((byte)208, color.PaletteIndex);
    }

    [Fact]
    public void Truecolor_ProducesTruecolorKind()
    {
        AnsiColor color = AnsiColor.Truecolor(255, 136, 0);
        Assert.Equal(AnsiColorKind.Truecolor, color.Kind);
        Assert.Equal((byte)255, color.Red);
        Assert.Equal((byte)136, color.Green);
        Assert.Equal((byte)0, color.Blue);
    }

    [Theory]
    [InlineData(ConsoleColor.Black,       AnsiNamedColor.Black)]
    [InlineData(ConsoleColor.DarkRed,     AnsiNamedColor.Red)]
    [InlineData(ConsoleColor.DarkGreen,   AnsiNamedColor.Green)]
    [InlineData(ConsoleColor.DarkYellow,  AnsiNamedColor.Yellow)]
    [InlineData(ConsoleColor.DarkBlue,    AnsiNamedColor.Blue)]
    [InlineData(ConsoleColor.DarkMagenta, AnsiNamedColor.Magenta)]
    [InlineData(ConsoleColor.DarkCyan,    AnsiNamedColor.Cyan)]
    [InlineData(ConsoleColor.Gray,        AnsiNamedColor.White)]
    [InlineData(ConsoleColor.DarkGray,    AnsiNamedColor.BrightBlack)]
    [InlineData(ConsoleColor.Red,         AnsiNamedColor.BrightRed)]
    [InlineData(ConsoleColor.Green,       AnsiNamedColor.BrightGreen)]
    [InlineData(ConsoleColor.Yellow,      AnsiNamedColor.BrightYellow)]
    [InlineData(ConsoleColor.Blue,        AnsiNamedColor.BrightBlue)]
    [InlineData(ConsoleColor.Magenta,     AnsiNamedColor.BrightMagenta)]
    [InlineData(ConsoleColor.Cyan,        AnsiNamedColor.BrightCyan)]
    [InlineData(ConsoleColor.White,       AnsiNamedColor.BrightWhite)]
    public void ImplicitConversion_FromConsoleColor_MapsCorrectly(ConsoleColor input, AnsiNamedColor expected)
    {
        AnsiColor color = input;
        Assert.Equal(AnsiColorKind.Named, color.Kind);
        Assert.Equal(expected, color.NamedColor);
    }
}
```

- [ ] **Step 2: Run, confirm compile failure**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~AnsiColorTests
```

Expected: build error referencing `AnsiColor`.

- [ ] **Step 3: Create `AnsiColor.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public readonly record struct AnsiColor
{
    public AnsiColorKind Kind { get; init; }
    public byte Red { get; init; }
    public byte Green { get; init; }
    public byte Blue { get; init; }
    public byte PaletteIndex { get; init; }
    public AnsiNamedColor NamedColor { get; init; }

    public static AnsiColor Named(AnsiNamedColor namedColor) => new()
    {
        Kind = AnsiColorKind.Named,
        NamedColor = namedColor
    };

    public static AnsiColor Palette(byte paletteIndex) => new()
    {
        Kind = AnsiColorKind.Palette,
        PaletteIndex = paletteIndex
    };

    public static AnsiColor Truecolor(byte red, byte green, byte blue) => new()
    {
        Kind = AnsiColorKind.Truecolor,
        Red = red,
        Green = green,
        Blue = blue
    };

    public static implicit operator AnsiColor(ConsoleColor consoleColor) => consoleColor switch
    {
        ConsoleColor.Black       => Named(AnsiNamedColor.Black),
        ConsoleColor.DarkRed     => Named(AnsiNamedColor.Red),
        ConsoleColor.DarkGreen   => Named(AnsiNamedColor.Green),
        ConsoleColor.DarkYellow  => Named(AnsiNamedColor.Yellow),
        ConsoleColor.DarkBlue    => Named(AnsiNamedColor.Blue),
        ConsoleColor.DarkMagenta => Named(AnsiNamedColor.Magenta),
        ConsoleColor.DarkCyan    => Named(AnsiNamedColor.Cyan),
        ConsoleColor.Gray        => Named(AnsiNamedColor.White),
        ConsoleColor.DarkGray    => Named(AnsiNamedColor.BrightBlack),
        ConsoleColor.Red         => Named(AnsiNamedColor.BrightRed),
        ConsoleColor.Green       => Named(AnsiNamedColor.BrightGreen),
        ConsoleColor.Yellow      => Named(AnsiNamedColor.BrightYellow),
        ConsoleColor.Blue        => Named(AnsiNamedColor.BrightBlue),
        ConsoleColor.Magenta     => Named(AnsiNamedColor.BrightMagenta),
        ConsoleColor.Cyan        => Named(AnsiNamedColor.BrightCyan),
        ConsoleColor.White       => Named(AnsiNamedColor.BrightWhite),
        _                        => Named(AnsiNamedColor.White)
    };
}
```

- [ ] **Step 4: Run tests, confirm all pass**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~AnsiColorTests
```

Expected: 19 passing tests (3 factory tests + 16 theory rows).

---

### Task 3: `AnsiStyle` record struct

**Files:**
- Create: `Rymote.Konzole/Styling/AnsiStyle.cs`

- [ ] **Step 1: Create `AnsiStyle.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public readonly record struct AnsiStyle
{
    public AnsiColor? Foreground { get; init; }
    public AnsiColor? Background { get; init; }
    public AnsiTextDecoration Decoration { get; init; }

    public static AnsiStyle Empty => default;

    public AnsiStyle MergeWith(AnsiStyle other) => new()
    {
        Foreground = other.Foreground ?? Foreground,
        Background = other.Background ?? Background,
        Decoration = Decoration | other.Decoration
    };
}
```

`MergeWith` is the composition operation used by the emitter when pushing a new style onto the stack: outer style + inner override. Inner foreground/background override; decoration flags OR together.

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

### Task 4: `AnsiOptions`

**Files:**
- Create: `Rymote.Konzole/Styling/AnsiOptions.cs`

- [ ] **Step 1: Create `AnsiOptions.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public sealed class AnsiOptions
{
    public AnsiStyle? BaseStyle { get; init; }
    public IReadOnlyDictionary<string, AnsiStyle>? SegmentPalette { get; init; }
    public bool AppendFinalReset { get; init; } = true;
}
```

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

## Phase 2 — Parser (TDD, green throughout)

### Task 5: `StyleToken` + `StyleTokenKind`

**Files:**
- Create: `Rymote.Konzole/Styling/StyleTokenKind.cs`
- Create: `Rymote.Konzole/Styling/StyleToken.cs`

- [ ] **Step 1: Create `StyleTokenKind.cs`**

```csharp
namespace Rymote.Konzole.Styling;

internal enum StyleTokenKind
{
    Text,
    OpenTag,
    CloseTag
}
```

- [ ] **Step 2: Create `StyleToken.cs`**

```csharp
namespace Rymote.Konzole.Styling;

internal readonly record struct StyleToken
{
    public StyleTokenKind Kind { get; init; }
    public string Text { get; init; }

    public static StyleToken TextRun(string text) => new() { Kind = StyleTokenKind.Text, Text = text };
    public static StyleToken Open(string body)    => new() { Kind = StyleTokenKind.OpenTag, Text = body };
    public static StyleToken Close()              => new() { Kind = StyleTokenKind.CloseTag, Text = string.Empty };
}
```

- [ ] **Step 3: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

### Task 6: `StyleParser` (TDD)

**Files:**
- Create: `Rymote.Konzole/Styling/StyleParser.cs`
- Create: `Rymote.Konzole.Tests/Styling/StyleParserTests.cs`

`StyleParser` is `internal`, so tests need access. The `InternalsVisibleTo("Rymote.Konzole.Tests")` was added in the previous overhaul — already present in the csproj.

- [ ] **Step 1: Write the failing tests**

Create `Rymote.Konzole.Tests/Styling/StyleParserTests.cs`:

```csharp
using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StyleParserTests
{
    [Fact]
    public void Tokenize_PlainText_ProducesSingleTextToken()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("hello");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_OpenTag_ExtractsBody()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red]error[/]");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("red", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal("error", tokens[1].Text);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[2].Kind);
    }

    [Fact]
    public void Tokenize_MultipleStyleTokensInOneTag_PreservesWhitespace()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[bold red]x[/]");
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("bold red", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_DoubleBracket_EscapesToLiteralOpenBracket()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[[red]");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal("red]", tokens[1].Text);
    }

    [Fact]
    public void Tokenize_NestedTags_ProducesOpenOpenTextCloseClose()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red][bold]x[/][/]");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("red", tokens[0].Text);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[1].Kind);
        Assert.Equal("bold", tokens[1].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[2].Kind);
        Assert.Equal("x", tokens[2].Text);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[3].Kind);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[4].Kind);
    }

    [Fact]
    public void Tokenize_BareCloseTag_ProducesCloseToken()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[/]");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.CloseTag, tokens[0].Kind);
    }

    [Fact]
    public void Tokenize_UnterminatedOpenBracket_TreatedAsLiteralText()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[red");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[red", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_EmptyBrackets_TreatedAsLiteralText()
    {
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[] hello");
        Assert.Single(tokens);
        Assert.Equal(StyleTokenKind.Text, tokens[0].Kind);
        Assert.Equal("[] hello", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_BracketsAroundContent_ProducesOpenTagRegardlessOfBodyValidity()
    {
        // Parser does not validate body — that's the emitter's job.
        // Any well-formed [...]  becomes an OpenTag; emitters decide what to do with the body.
        IReadOnlyList<StyleToken> tokens = StyleParser.Tokenize("[2026-05-21] message");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(StyleTokenKind.OpenTag, tokens[0].Kind);
        Assert.Equal("2026-05-21", tokens[0].Text);
        Assert.Equal(StyleTokenKind.Text, tokens[1].Kind);
        Assert.Equal(" message", tokens[1].Text);
    }
}
```

> **Design note for the implementer:** the parser is intentionally dumb about tag content. It identifies the SHAPE of tags (`[...]`, `[/]`, `[[`) and emits tokens. The downstream emitters (ANSI / Strip) decide whether `2026-05-21` is a valid style. This is what makes leniency work: an emitter that can't resolve `[2026-05-21]` re-renders it as literal `[2026-05-21]` in its output.

- [ ] **Step 2: Run, confirm compile failure**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~StyleParserTests
```

- [ ] **Step 3: Create `StyleParser.cs`**

```csharp
namespace Rymote.Konzole.Styling;

internal static class StyleParser
{
    public static IReadOnlyList<StyleToken> Tokenize(ReadOnlySpan<char> input)
    {
        List<StyleToken> tokens = new();
        int position = 0;
        int textRunStart = 0;

        while (position < input.Length)
        {
            char current = input[position];

            if (current != '[')
            {
                position++;
                continue;
            }

            if (position + 1 < input.Length && input[position + 1] == '[')
            {
                FlushTextRun(input, textRunStart, position, tokens);
                tokens.Add(StyleToken.TextRun("["));
                position += 2;
                textRunStart = position;
                continue;
            }

            if (position + 2 < input.Length && input[position + 1] == '/' && input[position + 2] == ']')
            {
                FlushTextRun(input, textRunStart, position, tokens);
                tokens.Add(StyleToken.Close());
                position += 3;
                textRunStart = position;
                continue;
            }

            int closeIndex = input.Slice(position + 1).IndexOf(']');
            if (closeIndex < 0)
            {
                position++;
                continue;
            }

            int bodyEnd = position + 1 + closeIndex;
            int bodyLength = bodyEnd - (position + 1);
            if (bodyLength == 0)
            {
                position++;
                continue;
            }

            ReadOnlySpan<char> body = input.Slice(position + 1, bodyLength);
            if (body.IndexOf('[') >= 0)
            {
                position++;
                continue;
            }

            FlushTextRun(input, textRunStart, position, tokens);
            tokens.Add(StyleToken.Open(body.ToString()));
            position = bodyEnd + 1;
            textRunStart = position;
        }

        FlushTextRun(input, textRunStart, input.Length, tokens);
        return tokens;
    }

    private static void FlushTextRun(ReadOnlySpan<char> input, int start, int end, List<StyleToken> tokens)
    {
        if (end <= start) return;
        tokens.Add(StyleToken.TextRun(input.Slice(start, end - start).ToString()));
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~StyleParserTests
```

Expected: 9 passing tests.

---

## Phase 3 — Emitters (TDD)

### Task 7: `AnsiEmitContext` + `AnsiEmitter` (TDD)

**Files:**
- Create: `Rymote.Konzole/Styling/AnsiEmitContext.cs`
- Create: `Rymote.Konzole/Styling/AnsiEmitter.cs`
- Create: `Rymote.Konzole.Tests/Styling/AnsiEmitterTests.cs`

- [ ] **Step 1: Create `AnsiEmitContext.cs`**

```csharp
namespace Rymote.Konzole.Styling;

internal sealed class AnsiEmitContext
{
    public IReadOnlyDictionary<string, AnsiStyle>? SegmentPalette { get; init; }
    public AnsiStyle BaseStyle { get; init; } = AnsiStyle.Empty;
    public bool AppendFinalReset { get; init; } = true;
}
```

- [ ] **Step 2: Write the failing tests**

Create `Rymote.Konzole.Tests/Styling/AnsiEmitterTests.cs`:

```csharp
using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class AnsiEmitterTests
{
    private const string Reset = "\x1b[0m";

    private static string Emit(string input, AnsiEmitContext? context = null) =>
        AnsiEmitter.Emit(StyleParser.Tokenize(input), context ?? new AnsiEmitContext());

    [Fact]
    public void Red_WrapsTextWithSGR31_AndReset()
    {
        Assert.Equal("\x1b[31merror" + Reset + Reset, Emit("[red]error[/]"));
    }

    [Fact]
    public void BoldRed_CombinesSGRCodes_BoldFirst()
    {
        Assert.Equal("\x1b[1;31mcritical" + Reset + Reset, Emit("[bold red]critical[/]"));
    }

    [Fact]
    public void Truecolor_UsesSGR38_2()
    {
        Assert.Equal("\x1b[38;2;255;136;0mwarn" + Reset + Reset, Emit("[#ff8800]warn[/]"));
    }

    [Fact]
    public void Color256_UsesSGR38_5()
    {
        Assert.Equal("\x1b[38;5;208mwarn" + Reset + Reset, Emit("[color:208]warn[/]"));
    }

    [Fact]
    public void NamedBackground_UsesSGR43()
    {
        Assert.Equal("\x1b[43mwarn" + Reset + Reset, Emit("[bg:yellow]warn[/]"));
    }

    [Fact]
    public void HexBackground_UsesSGR48_2()
    {
        Assert.Equal("\x1b[48;2;255;136;0mwarn" + Reset + Reset, Emit("[bg:#ff8800]warn[/]"));
    }

    [Fact]
    public void Italic_Underline_Dim_Reverse_AllMapToTheirSGR()
    {
        Assert.Contains("\x1b[3m", Emit("[italic]x[/]"));
        Assert.Contains("\x1b[4m", Emit("[underline]x[/]"));
        Assert.Contains("\x1b[2m", Emit("[dim]x[/]"));
        Assert.Contains("\x1b[7m", Emit("[reverse]x[/]"));
    }

    [Fact]
    public void UnknownTag_PassesThroughAsLiteral()
    {
        // [Login] is not a valid style list, no segment palette → render literally
        Assert.Equal("[Login] complete", Emit("[Login] complete"));
    }

    [Fact]
    public void UnknownTag_WithCloseAfter_StillLiteral_BothBracketsAndCloseShown()
    {
        // Falls through entirely — the trailing [/] is dropped as a bare close.
        Assert.Equal("[Login] complete", Emit("[Login] complete[/]"));
    }

    [Fact]
    public void BaseStyle_Applied_AndRestoredAfterCloseTag()
    {
        AnsiEmitContext context = new()
        {
            BaseStyle = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) }
        };
        string result = AnsiEmitter.Emit(StyleParser.Tokenize("hello [bold]you[/] there"), context);
        // Should: open cyan, "hello ", close+reopen cyan+bold, "you", close+reopen cyan, " there", reset.
        Assert.StartsWith("\x1b[36m", result);
        Assert.Contains("\x1b[1;36m", result);     // bold + cyan combined inside the tag
        Assert.EndsWith(Reset, result);
    }

    [Fact]
    public void SegmentPalette_ResolvesTagBody_BeforeStyleGrammar()
    {
        AnsiEmitContext context = new()
        {
            SegmentPalette = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
            {
                ["timestamp"] = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
            }
        };
        string result = AnsiEmitter.Emit(StyleParser.Tokenize("[timestamp]12:00:00[/]"), context);
        Assert.Contains("\x1b[90m", result);   // BrightBlack
        Assert.Contains("12:00:00", result);
    }

    [Fact]
    public void DoubleBracketEscape_ResultsInLiteralOpenBracket()
    {
        Assert.Equal("[red] text", Emit("[[red] text"));
    }

    [Fact]
    public void CaseInsensitive_TagRecognition()
    {
        Assert.Equal("\x1b[31mx" + Reset + Reset, Emit("[RED]x[/]"));
        Assert.Equal("\x1b[1;31mx" + Reset + Reset, Emit("[Bold Red]x[/]"));
    }

    [Fact]
    public void UnclosedTag_AtEndOfInput_StillResetsAtEnd()
    {
        string result = Emit("[red]unclosed");
        Assert.StartsWith("\x1b[31m", result);
        Assert.EndsWith(Reset, result);
    }
}
```

> **Design note for the implementer:** SGR codes:
> - Foreground named: 30 + color index (0=black, 1=red, ..., 7=white). Bright: 90 + index.
> - Background named: 40 + color index. Bright: 100 + index.
> - 256-color foreground: `38;5;N`. Background: `48;5;N`.
> - Truecolor foreground: `38;2;R;G;B`. Background: `48;2;R;G;B`.
> - Decoration: bold=1, dim=2, italic=3, underline=4, reverse=7.
> - Combine multiple codes with `;` inside a single SGR, e.g. `\x1b[1;31m` for bold red.
> - Reset: `\x1b[0m`.
>
> Emit order inside an SGR sequence: decoration codes first (in flag order: bold, dim, italic, underline, reverse), then foreground, then background.

- [ ] **Step 3: Run, confirm compile failure**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~AnsiEmitterTests
```

- [ ] **Step 4: Create `AnsiEmitter.cs`**

```csharp
using System.Globalization;
using System.Text;

namespace Rymote.Konzole.Styling;

internal static class AnsiEmitter
{
    private const string Esc = "\x1b[";
    private const string Reset = "\x1b[0m";

    public static string Emit(IReadOnlyList<StyleToken> tokens, AnsiEmitContext context)
    {
        StringBuilder output = new();
        Stack<AnsiStyle> styleStack = new();
        styleStack.Push(context.BaseStyle);

        if (!IsEmpty(context.BaseStyle))
            output.Append(EncodeSgr(context.BaseStyle));

        foreach (StyleToken token in tokens)
        {
            switch (token.Kind)
            {
                case StyleTokenKind.Text:
                    output.Append(token.Text);
                    break;

                case StyleTokenKind.OpenTag:
                    if (!TryResolveStyle(token.Text, context.SegmentPalette, out AnsiStyle resolved))
                    {
                        output.Append('[').Append(token.Text).Append(']');
                        break;
                    }
                    AnsiStyle effective = styleStack.Peek().MergeWith(resolved);
                    styleStack.Push(effective);
                    output.Append(Reset).Append(EncodeSgr(effective));
                    break;

                case StyleTokenKind.CloseTag:
                    if (styleStack.Count <= 1) break;
                    styleStack.Pop();
                    AnsiStyle restored = styleStack.Peek();
                    output.Append(Reset);
                    if (!IsEmpty(restored)) output.Append(EncodeSgr(restored));
                    break;
            }
        }

        if (context.AppendFinalReset) output.Append(Reset);
        return output.ToString();
    }

    private static bool IsEmpty(AnsiStyle style) =>
        style.Foreground == null && style.Background == null && style.Decoration == AnsiTextDecoration.None;

    private static string EncodeSgr(AnsiStyle style)
    {
        List<string> codes = new();

        if ((style.Decoration & AnsiTextDecoration.Bold) != 0) codes.Add("1");
        if ((style.Decoration & AnsiTextDecoration.Dim) != 0) codes.Add("2");
        if ((style.Decoration & AnsiTextDecoration.Italic) != 0) codes.Add("3");
        if ((style.Decoration & AnsiTextDecoration.Underline) != 0) codes.Add("4");
        if ((style.Decoration & AnsiTextDecoration.Reverse) != 0) codes.Add("7");

        if (style.Foreground.HasValue) codes.Add(EncodeColor(style.Foreground.Value, isBackground: false));
        if (style.Background.HasValue) codes.Add(EncodeColor(style.Background.Value, isBackground: true));

        return codes.Count == 0 ? string.Empty : Esc + string.Join(';', codes) + "m";
    }

    private static string EncodeColor(AnsiColor color, bool isBackground)
    {
        int baseCode = isBackground ? 40 : 30;
        int brightCode = isBackground ? 100 : 90;
        int extendedCode = isBackground ? 48 : 38;

        return color.Kind switch
        {
            AnsiColorKind.Named => color.NamedColor switch
            {
                AnsiNamedColor.Black         => (baseCode + 0).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Red           => (baseCode + 1).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Green         => (baseCode + 2).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Yellow        => (baseCode + 3).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Blue          => (baseCode + 4).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Magenta       => (baseCode + 5).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.Cyan          => (baseCode + 6).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.White         => (baseCode + 7).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightBlack   => (brightCode + 0).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightRed     => (brightCode + 1).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightGreen   => (brightCode + 2).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightYellow  => (brightCode + 3).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightBlue    => (brightCode + 4).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightMagenta => (brightCode + 5).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightCyan    => (brightCode + 6).ToString(CultureInfo.InvariantCulture),
                AnsiNamedColor.BrightWhite   => (brightCode + 7).ToString(CultureInfo.InvariantCulture),
                _                            => (baseCode + 7).ToString(CultureInfo.InvariantCulture)
            },
            AnsiColorKind.Palette   => $"{extendedCode};5;{color.PaletteIndex}",
            AnsiColorKind.Truecolor => $"{extendedCode};2;{color.Red};{color.Green};{color.Blue}",
            _                        => string.Empty
        };
    }

    private static bool TryResolveStyle(
        string tagBody,
        IReadOnlyDictionary<string, AnsiStyle>? segmentPalette,
        out AnsiStyle style)
    {
        if (segmentPalette != null && segmentPalette.TryGetValue(tagBody, out style))
            return true;

        return StyleGrammar.TryParse(tagBody, out style);
    }
}
```

> **Design note for the implementer:** `StyleGrammar.TryParse` is created in the next sub-step. It parses a tag body like `"bold red"` or `"bg:#ff8800"` into an `AnsiStyle`, returning `false` for unrecognized content.

- [ ] **Step 5: Add `StyleGrammar` (helper used by `AnsiEmitter`)**

This is a private-to-the-styling-namespace helper used internally by both the ANSI emitter and the strip emitter to detect whether a tag body is a valid style. Add it as a NEW FILE so the one-OOP-per-file rule holds:

Create `Rymote.Konzole/Styling/StyleGrammar.cs`:

```csharp
using System.Globalization;

namespace Rymote.Konzole.Styling;

internal static class StyleGrammar
{
    public static bool TryParse(string tagBody, out AnsiStyle style)
    {
        style = AnsiStyle.Empty;
        if (string.IsNullOrWhiteSpace(tagBody)) return false;

        string[] tokens = tagBody.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        AnsiColor? foreground = null;
        AnsiColor? background = null;
        AnsiTextDecoration decoration = AnsiTextDecoration.None;

        foreach (string raw in tokens)
        {
            string token = raw.ToLowerInvariant();

            if (TryParseDecoration(token, out AnsiTextDecoration dec))
            {
                decoration |= dec;
                continue;
            }

            bool isBackground = token.StartsWith("bg:", StringComparison.Ordinal);
            string colorToken = isBackground ? token.Substring(3) : token;

            if (!TryParseColor(colorToken, out AnsiColor color))
                return false;

            if (isBackground) background = color;
            else foreground = color;
        }

        style = new AnsiStyle { Foreground = foreground, Background = background, Decoration = decoration };
        return foreground != null || background != null || decoration != AnsiTextDecoration.None;
    }

    private static bool TryParseDecoration(string token, out AnsiTextDecoration decoration)
    {
        switch (token)
        {
            case "bold":      decoration = AnsiTextDecoration.Bold;      return true;
            case "italic":    decoration = AnsiTextDecoration.Italic;    return true;
            case "underline": decoration = AnsiTextDecoration.Underline; return true;
            case "dim":       decoration = AnsiTextDecoration.Dim;       return true;
            case "reverse":   decoration = AnsiTextDecoration.Reverse;   return true;
            default:          decoration = AnsiTextDecoration.None;      return false;
        }
    }

    private static bool TryParseColor(string token, out AnsiColor color)
    {
        if (TryParseNamed(token, out AnsiNamedColor named))
        {
            color = AnsiColor.Named(named);
            return true;
        }

        if (token.StartsWith("color:", StringComparison.Ordinal))
        {
            if (byte.TryParse(token.AsSpan(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte paletteIndex))
            {
                color = AnsiColor.Palette(paletteIndex);
                return true;
            }
        }

        if (token.Length == 7 && token[0] == '#'
            && byte.TryParse(token.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte red)
            && byte.TryParse(token.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte green)
            && byte.TryParse(token.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte blue))
        {
            color = AnsiColor.Truecolor(red, green, blue);
            return true;
        }

        color = default;
        return false;
    }

    private static bool TryParseNamed(string token, out AnsiNamedColor namedColor)
    {
        switch (token)
        {
            case "black":          namedColor = AnsiNamedColor.Black;         return true;
            case "red":            namedColor = AnsiNamedColor.Red;           return true;
            case "green":          namedColor = AnsiNamedColor.Green;         return true;
            case "yellow":         namedColor = AnsiNamedColor.Yellow;        return true;
            case "blue":           namedColor = AnsiNamedColor.Blue;          return true;
            case "magenta":        namedColor = AnsiNamedColor.Magenta;       return true;
            case "cyan":           namedColor = AnsiNamedColor.Cyan;          return true;
            case "white":          namedColor = AnsiNamedColor.White;         return true;
            case "bright_black":   namedColor = AnsiNamedColor.BrightBlack;   return true;
            case "bright_red":     namedColor = AnsiNamedColor.BrightRed;     return true;
            case "bright_green":   namedColor = AnsiNamedColor.BrightGreen;   return true;
            case "bright_yellow":  namedColor = AnsiNamedColor.BrightYellow;  return true;
            case "bright_blue":    namedColor = AnsiNamedColor.BrightBlue;    return true;
            case "bright_magenta": namedColor = AnsiNamedColor.BrightMagenta; return true;
            case "bright_cyan":    namedColor = AnsiNamedColor.BrightCyan;    return true;
            case "bright_white":   namedColor = AnsiNamedColor.BrightWhite;   return true;
            default:               namedColor = default;                      return false;
        }
    }
}
```

> Update the **File map** at the top mentally — `StyleGrammar.cs` is created here.

- [ ] **Step 6: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~AnsiEmitterTests
```

Expected: 14 passing tests.

---

### Task 8: `StripEmitter` (TDD)

**Files:**
- Create: `Rymote.Konzole/Styling/StripEmitter.cs`
- Create: `Rymote.Konzole.Tests/Styling/StripEmitterTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StripEmitterTests
{
    private static string Strip(string input) => StripEmitter.Emit(StyleParser.Tokenize(input));

    [Fact] public void RemovesValidTags() => Assert.Equal("error", Strip("[red]error[/]"));

    [Fact] public void KeepsUnknownTagsAsLiteral_OutsideValidGrammar()
        => Assert.Equal("[Login] complete", Strip("[Login] complete"));

    [Fact] public void KeepsDateBrackets_AsLiteral()
        => Assert.Equal("[2026-05-21] message", Strip("[2026-05-21] message"));

    [Fact] public void ResolvesDoubleBracketEscape_ToLiteralBracket()
        => Assert.Equal("[red] text", Strip("[[red] text"));

    [Fact] public void DropsBareCloseTag() => Assert.Equal("text", Strip("text[/]"));

    [Fact] public void NestedValidTags_FullyStripped()
        => Assert.Equal("error CRITICAL details", Strip("[red]error [bold]CRITICAL[/] details[/]"));

    [Fact] public void HexAndBackgroundTags_Stripped()
        => Assert.Equal("warn", Strip("[bg:#ff8800 white]warn[/]"));
}
```

- [ ] **Step 2: Create `StripEmitter.cs`**

```csharp
using System.Text;

namespace Rymote.Konzole.Styling;

internal static class StripEmitter
{
    public static string Emit(IReadOnlyList<StyleToken> tokens)
    {
        StringBuilder output = new();

        foreach (StyleToken token in tokens)
        {
            switch (token.Kind)
            {
                case StyleTokenKind.Text:
                    output.Append(token.Text);
                    break;

                case StyleTokenKind.OpenTag:
                    if (StyleGrammar.TryParse(token.Text, out _)) break;
                    output.Append('[').Append(token.Text).Append(']');
                    break;

                case StyleTokenKind.CloseTag:
                    break;
            }
        }

        return output.ToString();
    }
}
```

- [ ] **Step 3: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~StripEmitterTests
```

Expected: 7 passing tests.

---

## Phase 4 — Public facade

### Task 9: `StyleMarkup` (TDD)

**Files:**
- Create: `Rymote.Konzole/Styling/StyleMarkup.cs`
- Create: `Rymote.Konzole.Tests/Styling/StyleMarkupTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using Rymote.Konzole.Styling;
using Xunit;

namespace Rymote.Konzole.Tests.Styling;

public class StyleMarkupTests
{
    [Fact]
    public void ToAnsi_NoOptions_ConvertsBasicTag()
    {
        Assert.Equal("\x1b[31merror\x1b[0m\x1b[0m", StyleMarkup.ToAnsi("[red]error[/]"));
    }

    [Fact]
    public void ToAnsi_WithBaseStyle_AppliesAround()
    {
        AnsiOptions options = new() { BaseStyle = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.Cyan) } };
        string result = StyleMarkup.ToAnsi("hello", options);
        Assert.StartsWith("\x1b[36m", result);
        Assert.EndsWith("\x1b[0m", result);
        Assert.Contains("hello", result);
    }

    [Fact]
    public void ToAnsi_WithSegmentPalette_ResolvesNamedSegment()
    {
        AnsiOptions options = new()
        {
            SegmentPalette = new Dictionary<string, AnsiStyle>(StringComparer.OrdinalIgnoreCase)
            {
                ["timestamp"] = new AnsiStyle { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
            }
        };
        string result = StyleMarkup.ToAnsi("[timestamp]12:00:00[/]", options);
        Assert.Contains("\x1b[90m", result);
        Assert.Contains("12:00:00", result);
    }

    [Fact]
    public void Strip_RemovesTagsAndKeepsLiterals()
    {
        Assert.Equal("[2026-05-21] error", StyleMarkup.Strip("[2026-05-21] [red]error[/]"));
    }

    [Fact]
    public void Strip_LeavesPlainText_Untouched()
    {
        Assert.Equal("plain text", StyleMarkup.Strip("plain text"));
    }
}
```

- [ ] **Step 2: Create `StyleMarkup.cs`**

```csharp
namespace Rymote.Konzole.Styling;

public static class StyleMarkup
{
    public static string ToAnsi(string input)
    {
        return AnsiEmitter.Emit(StyleParser.Tokenize(input), new AnsiEmitContext());
    }

    public static string ToAnsi(string input, AnsiOptions options)
    {
        AnsiEmitContext context = new()
        {
            BaseStyle = options.BaseStyle ?? AnsiStyle.Empty,
            SegmentPalette = options.SegmentPalette,
            AppendFinalReset = options.AppendFinalReset
        };
        return AnsiEmitter.Emit(StyleParser.Tokenize(input), context);
    }

    public static string Strip(string input)
    {
        return StripEmitter.Emit(StyleParser.Tokenize(input));
    }
}
```

- [ ] **Step 3: Run tests + entire test suite to confirm nothing broke**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj
```

Expected: 5 new tests pass; previous 40+ tests still pass.

---

## Phase 5 — Strip in non-console sinks

### Task 10: Add `FormatterHelpers.StripStyles`

**Files:**
- Modify: `Rymote.Konzole/Formatters/FormatterHelpers.cs`

- [ ] **Step 1: Add the helper**

In `Rymote.Konzole/Formatters/FormatterHelpers.cs`, add a `using Rymote.Konzole.Styling;` at the top and insert this method anywhere inside the class (e.g., right after `TruncateMessage`):

```csharp
public static string StripStyles(string text) => StyleMarkup.Strip(text);
```

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors.

---

### Task 11: `JsonFormatter` strips user-content fields (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/JsonFormatter.cs`
- Modify: `Rymote.Konzole.Tests/Formatters/JsonFormatterTests.cs`

- [ ] **Step 1: Add a failing test**

Append to `Rymote.Konzole.Tests/Formatters/JsonFormatterTests.cs`:

```csharp
[Fact]
public void Format_StripsStyleMarkup_FromMessage()
{
    JsonFormatter formatter = new();
    LogEntry entry = new() { Level = LogLevel.Information, Message = "[red]warn[/]" };
    string json = formatter.Format(entry, DefaultContext);
    using JsonDocument document = JsonDocument.Parse(json);
    Assert.Equal("warn", document.RootElement.GetProperty("message").GetString());
}

[Fact]
public void Format_StripsStyleMarkup_FromExceptionMessage()
{
    JsonFormatter formatter = new();
    InvalidOperationException exception = new("[bold]boom[/]");
    LogEntry entry = new() { Level = LogLevel.Error, Message = "failed", Exception = exception };
    string json = formatter.Format(entry, DefaultContext);
    using JsonDocument document = JsonDocument.Parse(json);
    Assert.Equal("boom", document.RootElement.GetProperty("exception").GetProperty("message").GetString());
}
```

- [ ] **Step 2: Modify `JsonFormatter.cs` to strip user-content**

In `Rymote.Konzole/Formatters/JsonFormatter.cs`, inside `Format(LogEntry, FormatterContext)`, change the lines that put `entry.Message` and `entry.Exception?.Message` into the document:

```csharp
public string Format(LogEntry entry, FormatterContext context)
{
    Dictionary<string, object?> document = new()
    {
        ["timestamp"] = context.ShowTimestamp ? entry.Timestamp.ToString(context.TimestampFormat) : null,
        ["level"] = entry.Level.ToString(),
        ["tag"] = entry.Tag?.ToString(),
        ["message"] = FormatterHelpers.StripStyles(entry.Message),
        ["category"] = context.ShowCategory ? entry.Category : null,
        ["eventId"] = context.ShowEventId && entry.EventId.Id != 0 ? entry.EventId.Id : (int?)null,
        ["eventName"] = context.ShowEventId ? entry.EventId.Name : null,
        ["exception"] = context.ShowException && entry.Exception != null
            ? StripExceptionMarkup(entry.Exception)
            : null,
        ["properties"] = entry.Properties,
        ["scope"] = context.ShowScope ? entry.Scope : null,
        ["traceId"] = entry.TraceId,
        ["spanId"] = entry.SpanId
    };

    return JsonSerializer.Serialize(document, _jsonSerializerOptions);
}

private static Exception StripExceptionMarkup(Exception original)
{
    if (original.Message.IndexOf('[') < 0 && original.InnerException == null) return original;
    return new MarkupStrippedException(original);
}

private sealed class MarkupStrippedException : Exception
{
    private readonly Exception _inner;

    public MarkupStrippedException(Exception inner) : base(FormatterHelpers.StripStyles(inner.Message))
    {
        _inner = inner;
    }

    public override string? StackTrace => _inner.StackTrace;
    public override Exception? InnerException => _inner.InnerException == null
        ? null
        : new MarkupStrippedException(_inner.InnerException);
}
```

> **Design note for the implementer:** the `ExceptionJsonConverter` (created in the v2.0 overhaul) serializes the exception via reflection of `Message`, `StackTrace`, `InnerException`. Substituting a wrapper that returns a stripped `Message` and recursively wraps `InnerException` is the cleanest way to get stripping without rewriting the converter.

- [ ] **Step 3: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~JsonFormatterTests
```

Expected: all `JsonFormatterTests` pass, including the two new ones.

---

### Task 12: `DiscordFormatter` strips Message + Category (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/DiscordFormatter.cs`
- Modify: `Rymote.Konzole.Tests/Formatters/DiscordFormatterTests.cs`

- [ ] **Step 1: Add a failing test**

Append to `Rymote.Konzole.Tests/Formatters/DiscordFormatterTests.cs`:

```csharp
[Fact]
public void Format_StripsStyleMarkup_FromMessage()
{
    DiscordFormatter formatter = new();
    LogEntry entry = new() { Level = LogLevel.Warning, Message = "[red]careful[/]" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.DoesNotContain("[red]", rendered);
    Assert.DoesNotContain("[/]", rendered);
    Assert.Contains("careful", rendered);
}
```

- [ ] **Step 2: Modify `DiscordFormatter.cs`**

Replace the line that appends `entry.Message` (currently `stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));`) with:

```csharp
string messagePlain = FormatterHelpers.StripStyles(entry.Message);
stringBuilder.Append(FormatterHelpers.TruncateMessage(messagePlain, context.MaxMessageLength));
```

If the formatter also embeds `entry.Category` (it does — inside `[ ]` brackets), no strip needed there because category strings are framework-supplied and don't contain user markup.

- [ ] **Step 3: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~DiscordFormatterTests
```

Expected: all `DiscordFormatterTests` pass.

---

### Task 13: `SlackFormatter` strips Message (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/SlackFormatter.cs`
- Modify: `Rymote.Konzole.Tests/Formatters/SlackFormatterTests.cs`

- [ ] **Step 1: Add a failing test**

Append to `Rymote.Konzole.Tests/Formatters/SlackFormatterTests.cs`:

```csharp
[Fact]
public void Format_StripsStyleMarkup_FromMessage()
{
    SlackFormatter formatter = new();
    LogEntry entry = new() { Level = LogLevel.Error, Message = "[bold]boom[/]" };
    string rendered = formatter.Format(entry, PlainContext);
    Assert.DoesNotContain("[bold]", rendered);
    Assert.DoesNotContain("[/]", rendered);
    Assert.Contains("boom", rendered);
}
```

- [ ] **Step 2: Modify `SlackFormatter.cs`**

Replace the line `stringBuilder.Append(FormatterHelpers.TruncateMessage(entry.Message, context.MaxMessageLength));` with:

```csharp
string messagePlain = FormatterHelpers.StripStyles(entry.Message);
stringBuilder.Append(FormatterHelpers.TruncateMessage(messagePlain, context.MaxMessageLength));
```

- [ ] **Step 3: Run tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~SlackFormatterTests
```

Expected: all `SlackFormatterTests` pass.

---

## Phase 6 — Console rendering rewrite

### Task 14: `ConsoleSinkOptions` — `AnsiColor` and `SegmentStyles`

**Files:**
- Modify: `Rymote.Konzole/Configuration/ConsoleSinkOptions.cs`

- [ ] **Step 1: Replace contents**

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Models;
using Rymote.Konzole.Styling;

namespace Rymote.Konzole.Configuration;

public sealed class ConsoleSinkOptions : SinkOptionsBase
{
    public bool UseColors { get; set; } = true;
    public bool UseEmojis { get; set; } = true;

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
        [ConsoleSegment.PropertyKey]      = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) },
        [ConsoleSegment.PropertyValue]    = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightWhite) },
        [ConsoleSegment.ExceptionLabel]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red), Decoration = AnsiTextDecoration.Bold },
        [ConsoleSegment.ExceptionMessage] = new() { Foreground = AnsiColor.Named(AnsiNamedColor.Red) },
        [ConsoleSegment.ExceptionStack]   = new() { Foreground = AnsiColor.Named(AnsiNamedColor.BrightBlack) }
    };
}
```

> **Design note for the implementer:** the previous `ConsoleColor`-typed maps are now `AnsiColor`. Users who set them via `ConsoleColor.Red` keep compiling because of the implicit conversion added in Task 2. The `CriticalBackgroundColor` type changes from `ConsoleColor` to `AnsiColor` — same implicit-conversion compatibility.

- [ ] **Step 2: Build**

```bash
dotnet build Rymote.Konzole/Rymote.Konzole.csproj
```

Expected: 0 errors. `ConsoleSink` and `ConsoleFormatter` will be temporarily broken on `Console.ForegroundColor = levelColor` lines — that's fine because Task 15 and 16 rewrite them next. If you want incremental green, run Task 15 and 16 in one session.

---

### Task 15: `ConsoleFormatter` — emit segment-wrapped markup (TDD)

**Files:**
- Modify: `Rymote.Konzole/Formatters/ConsoleFormatter.cs`
- Create: `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Rymote.Konzole.Tests/Formatters/ConsoleFormatterStyledTests.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;
using Xunit;

namespace Rymote.Konzole.Tests.Formatters;

public class ConsoleFormatterStyledTests
{
    private static readonly FormatterContext PlainContext = new()
    {
        ShowTimestamp = false,
        ShowCategory = false,
        ShowScope = false,
        ShowException = false
    };

    [Fact]
    public void Format_InformationMessage_WrapsInMessageSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "hello" };

        string rendered = formatter.Format(entry, PlainContext);

        Assert.Contains("[message]hello[/]", rendered);
        Assert.DoesNotContain("\x1b[", rendered);   // no ANSI yet — that's the sink's job
    }

    [Fact]
    public void Format_WarningMessage_WrapsInMessageWarningSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Warning, Message = "watch out" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message-warning]watch out[/]", rendered);
    }

    [Fact]
    public void Format_ErrorMessage_WrapsInMessageErrorSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Error, Message = "boom" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message-error]boom[/]", rendered);
    }

    [Fact]
    public void Format_PreservesUserInlineMarkup_InsideMessageSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        LogEntry entry = new() { Level = LogLevel.Information, Message = "User [bold]Alice[/] joined" };
        string rendered = formatter.Format(entry, PlainContext);
        Assert.Contains("[message]User [bold]Alice[/] joined[/]", rendered);
    }

    [Fact]
    public void Format_WithTimestamp_WrapsTimestampInTimestampSegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { ShowTimestamp = true, ShowCategory = false, ShowScope = false, ShowException = false };
        LogEntry entry = new()
        {
            Level = LogLevel.Information,
            Timestamp = new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.Zero),
            Message = "x"
        };
        string rendered = formatter.Format(entry, context);
        Assert.Contains("[timestamp]", rendered);
        Assert.Contains("2026-05-21 12:00:00", rendered);
    }

    [Fact]
    public void Format_WithCategory_WrapsInCategorySegment()
    {
        ConsoleFormatter formatter = new(useEmojis: false);
        FormatterContext context = new() { ShowTimestamp = false, ShowCategory = true, ShowScope = false, ShowException = false };
        LogEntry entry = new() { Level = LogLevel.Information, Category = "App.Service", Message = "x" };
        string rendered = formatter.Format(entry, context);
        Assert.Contains("[category]", rendered);
        Assert.Contains("App.Service", rendered);
    }
}
```

- [ ] **Step 2: Replace `Rymote.Konzole/Formatters/ConsoleFormatter.cs`**

```csharp
using System.Text;
using Microsoft.Extensions.Logging;
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

        stringBuilder.Append("[icon]");
        if (renderEmoji)
        {
            stringBuilder.Append(entry.Tag.HasValue
                ? LogIcon.GetIcon(entry.Tag.Value)
                : LogIcon.GetIcon(entry.Level));
            stringBuilder.Append("[/]  ");
        }
        else
        {
            stringBuilder.Append(entry.Tag.HasValue
                ? LogIcon.GetFallbackIcon(entry.Tag.Value)
                : LogIcon.GetFallbackIcon(entry.Level));
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

> **Design note for the implementer:** The event-id segment uses `[event-id]` (kebab case) for the markup key but maps to `ConsoleSegment.EventId` (Pascal case) in the enum. The sink builds a Dictionary keyed by the markup strings — see Task 16's `SegmentToMarkupKey`.

- [ ] **Step 3: Run the new tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj --filter FullyQualifiedName~ConsoleFormatterStyledTests
```

Expected: 6 passing tests. The existing `ConsoleSinkTests` will still fail until Task 16 — that's expected (formatter output now contains tags).

---

### Task 16: `ConsoleSink` — palette-driven ANSI emission

**Files:**
- Modify: `Rymote.Konzole/Sinks/ConsoleSink.cs`
- Modify: `Rymote.Konzole.Tests/Sinks/ConsoleSinkTests.cs`

- [ ] **Step 1: Add ANSI emission tests to `ConsoleSinkTests.cs`**

Append:

```csharp
[Fact]
public async Task EmitsAnsiEscapes_ForInformationLevel()
{
    StringWriter capturedStandardOut = new();
    TextWriter originalStandardOut = Console.Out;
    Console.SetOut(capturedStandardOut);

    try
    {
        ConsoleSinkOptions options = new() { UseColors = true, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
        await using ConsoleSink sink = new(options);

        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "hello" });
        await sink.FlushAsync(CancellationToken.None);
    }
    finally
    {
        Console.SetOut(originalStandardOut);
    }

    string output = capturedStandardOut.ToString();
    Assert.Contains("\x1b[", output);   // any ANSI escape present
    Assert.Contains("hello", output);
    Assert.DoesNotContain("[message]", output);  // markup was converted, not literal
}

[Fact]
public async Task EmitsAnsiEscapes_ForUserInlineMarkup()
{
    StringWriter capturedStandardOut = new();
    TextWriter originalStandardOut = Console.Out;
    Console.SetOut(capturedStandardOut);

    try
    {
        ConsoleSinkOptions options = new() { UseColors = true, UseEmojis = false, ShowTimestamp = false, ShowCategory = false };
        await using ConsoleSink sink = new(options);

        sink.TryEnqueue(new LogEntry { Level = LogLevel.Information, Message = "User [bold]Alice[/] in" });
        await sink.FlushAsync(CancellationToken.None);
    }
    finally
    {
        Console.SetOut(originalStandardOut);
    }

    string output = capturedStandardOut.ToString();
    Assert.Contains("\x1b[1m", output);  // bold
    Assert.Contains("Alice", output);
    Assert.DoesNotContain("[bold]", output);
    Assert.DoesNotContain("[/]", output);
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

    protected override ILogFormatter CreateDefaultFormatter() => new ConsoleFormatter(Options.UseEmojis);

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
                SegmentPalette = ExtendPaletteWithIconAndCritical(entry)
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

    private IReadOnlyDictionary<string, AnsiStyle> ExtendPaletteWithIconAndCritical(LogEntry entry)
    {
        Dictionary<string, AnsiStyle> palette = new(_segmentPalette, StringComparer.OrdinalIgnoreCase);

        AnsiStyle iconStyle = new()
        {
            Foreground = ResolveIconColor(entry)
        };
        palette["icon"] = iconStyle;

        if (entry.Level == LogLevel.Critical)
        {
            AnsiStyle messageErrorStyle = palette.TryGetValue("message-error", out AnsiStyle existing) ? existing : AnsiStyle.Empty;
            palette["message-error"] = messageErrorStyle with { Background = Options.CriticalBackgroundColor };
        }

        return palette;
    }

    private AnsiColor? ResolveIconColor(LogEntry entry)
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
        ConsoleSegment.PropertyKey      => "property-key",
        ConsoleSegment.PropertyValue    => "property-value",
        ConsoleSegment.ExceptionLabel   => "exception-label",
        ConsoleSegment.ExceptionMessage => "exception-message",
        ConsoleSegment.ExceptionStack   => "exception-stack",
        _                               => segment.ToString().ToLowerInvariant()
    };
}
```

> **Design note for the implementer:** when `UseColors = false`, we still strip the markup so the output is plain text without the `[message]` etc. wrappers. The existing tests that asserted stdout contained "hello-stdout" still pass because Strip leaves the message body intact.

- [ ] **Step 3: Run all tests**

```bash
dotnet test Rymote.Konzole.Tests/Rymote.Konzole.Tests.csproj
```

Expected: every test passes, including the two new ANSI emission tests, the six new `ConsoleFormatterStyledTests`, and all pre-existing tests.

---

## Phase 7 — Final verification and version bump

### Task 17: Bump version to 2.1.0 and run full suite

**Files:**
- Modify: `Rymote.Konzole/Rymote.Konzole.csproj`

- [ ] **Step 1: Edit the version line**

In `Rymote.Konzole/Rymote.Konzole.csproj`, change:

```xml
<Version>2.0.1</Version>
```

to:

```xml
<Version>2.1.0</Version>
```

- [ ] **Step 2: Full build**

```bash
dotnet build Rymote.Konzole.sln
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Full test run**

```bash
dotnet test Rymote.Konzole.sln
```

Expected: all tests pass (the prior 40-ish plus ~40 new styling/formatter/sink tests).

- [ ] **Step 4: Commit (ask user first)**

```bash
git add Rymote.Konzole/ Rymote.Konzole.Tests/
git commit -m "Add inline style syntax for log messages (2.1.0)"
```

Suggested message body:

```
- BBCode-style markup [tag]content[/] in log messages
- ConsoleSink converts to ANSI escapes; other sinks strip
- Per-segment default palette for multi-color console output
- New types: StyleMarkup, AnsiStyle, AnsiColor, AnsiOptions, ConsoleSegment
- Bump 2.0.1 -> 2.1.0
```

---

## Self-review

Read the spec again with fresh eyes and checked the plan against it.

**1. Spec coverage**

| Spec section | Covered by |
|---|---|
| §3 Grammar (BBCode, escape, leniency) | Tasks 5-6 (parser), Task 7 step 5 (StyleGrammar) |
| §4.1 Files added | Tasks 1-9 cover all 14 styling files (StyleGrammar.cs added in Task 7 step 5 — see note in plan) |
| §4.2 Public surface (StyleMarkup, AnsiOptions, AnsiStyle, AnsiColor, AnsiColorKind, AnsiNamedColor, AnsiTextDecoration, ConsoleSegment) | Tasks 1-4, 9 |
| §4.3 Pipeline (parser → emitter) | Tasks 6-8 |
| §4.5 ConsoleColor → AnsiColor conversion | Task 2 |
| §5.1 Default per-segment palette | Task 14 |
| §5.2 ConsoleSinkOptions additions | Task 14 |
| §5.3 ConsoleFormatter — segment markup | Task 15 |
| §5.4 ConsoleSink — palette + ANSI emission | Task 16 |
| §6 Strip in non-console sinks | Tasks 10-13 |
| §7 Testing | Tests in Tasks 2, 6, 7, 8, 9, 11, 12, 13, 15, 16 |
| §8 Versioning (2.1.0) | Task 17 |

No gaps.

**2. Placeholder scan**

- No `TBD`, `TODO`, or "implement later" markers.
- Every code step shows complete code, including `StyleGrammar.cs` even though it wasn't in the original spec file map (added inline at Task 7 step 5 with a note).
- Every test step shows the actual test code, not "and similar tests for other styles".

**3. Type / method consistency**

Cross-checked names across tasks:

- `AnsiColor.Named(AnsiNamedColor)` / `.Palette(byte)` / `.Truecolor(byte, byte, byte)` (Task 2) — used in Tasks 7, 14. Consistent.
- `AnsiStyle.MergeWith(AnsiStyle)` (Task 3) — used in Task 7's `AnsiEmitter`. Consistent.
- `StyleToken.TextRun/Open/Close` factory methods (Task 5) — used in Task 6's `StyleParser`. Consistent.
- `StyleGrammar.TryParse(string, out AnsiStyle)` (Task 7 step 5) — used in `AnsiEmitter` (Task 7) and `StripEmitter` (Task 8). Consistent.
- `FormatterHelpers.StripStyles(string)` (Task 10) — used in Tasks 11, 12, 13. Consistent.
- `ConsoleSegment` enum keys (Task 1) — mapped to markup strings in Task 16's `SegmentToMarkupKey`. The mapping is the one place that bridges enum and string; every other reference uses the enum.
- `[message]`, `[message-warning]`, `[message-error]` markup keys — produced by `ConsoleFormatter` (Task 15), consumed by `SegmentToMarkupKey` (Task 16). Consistent.

**4. Other notes**

- The spec mentions `AnsiColor` has an `implicit operator AnsiColor(ConsoleColor)` — Task 2 implements it. The `ConsoleColor` → `AnsiNamedColor` mapping is duplicated in spec §4.5 and Task 2 — same values.
- The `[[red]]` test in spec §7.1 produces `Text("[")`, `Text("red]]")` — the parser test in Task 6 only covers `[[red]` (single trailing bracket); the double-trailing case is implicit from the parser logic and would pass without an additional test. Acceptable coverage.
- `MarkupStrippedException` (Task 11) is a private nested class inside `JsonFormatter` — exception to one-OOP-per-file because it's a defensive shim used only by `JsonFormatter`'s exception serialization. The previous overhaul allowed similar exceptions (`PopOnDispose` in `KonzoleScopeState`).

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-21-inline-style-syntax.md`. Two execution options:

**1. Subagent-Driven (recommended)** — Dispatches a fresh subagent per task, two-stage review between tasks, fast iteration. Uses `superpowers:subagent-driven-development`.

**2. Inline Execution** — Executes tasks in this session using `superpowers:executing-plans`, batched with checkpoints.

Which approach?
