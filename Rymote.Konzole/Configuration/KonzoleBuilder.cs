using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Sinks;

namespace Rymote.Konzole.Configuration;

public class KonzoleBuilder
{
    private readonly ILoggingBuilder _loggingBuilder;
    private readonly List<ISink> _sinks = new();
    
    public KonzoleBuilder(ILoggingBuilder loggingBuilder)
    {
        _loggingBuilder = loggingBuilder;
    }
    
    public KonzoleBuilder AddConsoleSink(Action<ConsoleSinkOptions>? configure = null)
    {
        ConsoleSinkOptions consoleSinkOptions = new ConsoleSinkOptions();
        configure?.Invoke(consoleSinkOptions);
        
        _sinks.Add(new ConsoleSink(consoleSinkOptions));
        return this;
    }
    
    public KonzoleBuilder AddFileSink(string? filePath = null, Action<FileSinkOptions>? configure = null)
    {
        FileSinkOptions fileSinkOptions = new FileSinkOptions();
        
        if (!string.IsNullOrEmpty(filePath))
        {
            fileSinkOptions.FilePath = filePath;
        }
        
        configure?.Invoke(fileSinkOptions);
        
        _sinks.Add(new FileSink(fileSinkOptions));
        return this;
    }
    
    public KonzoleBuilder AddRemoteSink(string endpoint, string? apiKey = null, Action<RemoteSinkOptions>? configure = null)
    {
        RemoteSinkOptions remoteSinkOptions = new RemoteSinkOptions();
        
        remoteSinkOptions.RemoteEndpoint = endpoint;
        remoteSinkOptions.RemoteApiKey = apiKey;
        
        configure?.Invoke(remoteSinkOptions);
        
        _sinks.Add(new RemoteSink(remoteSinkOptions));
        return this;
    }
    
    public KonzoleBuilder AddDiscordSink(string webhookUrl, Action<DiscordSinkOptions>? configure = null)
    {
        DiscordSinkOptions discordSinkOptions = new DiscordSinkOptions();
    
        discordSinkOptions.WebhookUrl = webhookUrl;
    
        configure?.Invoke(discordSinkOptions);
    
        _sinks.Add(new DiscordSink(discordSinkOptions));
        return this;
    }

    public KonzoleBuilder AddSlackSink(string webhookUrl, string? channel = null, Action<SlackSinkOptions>? configure = null)
    {
        SlackSinkOptions slackSinkOptions = new SlackSinkOptions();
    
        slackSinkOptions.WebhookUrl = webhookUrl;
        if (!string.IsNullOrEmpty(channel))
        {
            slackSinkOptions.Channel = channel;
        }
    
        configure?.Invoke(slackSinkOptions);
    
        _sinks.Add(new SlackSink(slackSinkOptions));
        return this;
    }
    
    public KonzoleBuilder AddSink(ISink sink)
    {
        _sinks.Add(sink);
        return this;
    }
    
    public KonzoleBuilder AddSink<TSink>() where TSink : ISink, new()
    {
        _sinks.Add(new TSink());
        return this;
    }
    
    public void Build()
    {
        if (!_sinks.Any())
        {
            throw new InvalidOperationException("At least one sink must be configured.");
        }
        
        // Register the provider with the configured sinks
        _loggingBuilder.Services.AddSingleton<ILoggerProvider>(serviceProvider => 
            new KonzoleLoggerProvider(_sinks));
    }
} 