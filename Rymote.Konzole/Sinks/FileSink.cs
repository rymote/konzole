using System.Collections.Concurrent;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Formatters;
using Rymote.Konzole.Models;

namespace Rymote.Konzole.Sinks;

public class FileSink : SinkBase<FileSinkOptions>
{
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Timer _flushTimer;
    private StreamWriter? _streamWriter;
    private string _currentFilePath;
    private long _currentFileSize;
    private int _fileIndex;
    
    public override string Name => "File";
    
    public FileSink(FileSinkOptions options) : base(options)
    {
        if (string.IsNullOrEmpty(Options.FilePath))
        {
            Options.FilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "konzole.log");
        }
        
        string? directoryPath = Path.GetDirectoryName(Options.FilePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        _currentFilePath = GetCurrentFilePath();
        InitializeWriter();
        
        _flushTimer = new Timer(_ => FlushAsync().Wait(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public override async Task WriteAsync(LogEntry entry)
    {
        if (!ShouldLog(entry))
            return;
            
        string formattedMessage = Formatter.Format(entry);
        _messageQueue.Enqueue(formattedMessage);
        
        if (_messageQueue.Count >= 10)
        {
            await FlushAsync();
        }
    }
    
    public override async Task FlushAsync()
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            List<string> entries = new List<string>();
            while (_messageQueue.TryDequeue(out string? entry))
            {
                entries.Add(entry);
            }
            
            if (entries.Count == 0)
                return;
                
            foreach (string entry in entries)
            {
                await WriteToFileAsync(entry);
            }
            
            await _streamWriter!.FlushAsync();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    
    protected override ILogFormatter CreateDefaultFormatter()
    {
        return new JsonFormatter(Options);
    }
    
    private async Task WriteToFileAsync(string entry)
    {
        int byteCount = System.Text.Encoding.UTF8.GetByteCount(entry) + Environment.NewLine.Length;
        
        if (_currentFileSize + byteCount > Options.MaxFileSize)
        {
            await RotateFileAsync();
        }
        
        await _streamWriter!.WriteLineAsync(entry);
        _currentFileSize += byteCount;
    }
    
    private async Task RotateFileAsync()
    {
        if (_streamWriter != null)
        {
            await _streamWriter.DisposeAsync();
        }
        
        _fileIndex++;
        if (_fileIndex >= Options.MaxFiles)
        {
            _fileIndex = 0;
        }
        
        _currentFilePath = GetCurrentFilePath();
        InitializeWriter();
        _currentFileSize = 0;
    }
    
    private string GetCurrentFilePath()
    {
        if (_fileIndex == 0)
            return Options.FilePath!;
            
        string directoryPath = Path.GetDirectoryName(Options.FilePath)!;
        string fileName = Path.GetFileNameWithoutExtension(Options.FilePath);
        string fileExtension = Path.GetExtension(Options.FilePath);
        
        return Path.Combine(directoryPath, $"{fileName}.{_fileIndex}{fileExtension}");
    }
    
    private void InitializeWriter()
    {
        _streamWriter = new StreamWriter(_currentFilePath, append: true)
        {
            AutoFlush = false
        };
        
        if (File.Exists(_currentFilePath))
        {
            _currentFileSize = new FileInfo(_currentFilePath).Length;
        }
    }
    
    public override void Dispose()
    {
        _flushTimer?.Dispose();
        base.Dispose();
        _streamWriter?.Dispose();
        _semaphoreSlim?.Dispose();
    }
} 