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
