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
