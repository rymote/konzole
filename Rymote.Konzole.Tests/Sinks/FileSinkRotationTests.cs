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
