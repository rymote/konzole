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
