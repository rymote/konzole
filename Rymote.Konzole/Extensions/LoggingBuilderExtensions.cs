using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder, Action<KonzoleBuilder> configure)
    {
        KonzoleBuilder konzoleBuilder = new KonzoleBuilder(builder);
        configure(konzoleBuilder);
        konzoleBuilder.Build();
        
        return builder;
    }
    
    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder)
    {
        return builder.AddKonzole(konzoleBuilder => konzoleBuilder.AddConsoleSink());
    }
    
    public static ILoggingBuilder AddKonzoleStdout(this ILoggingBuilder builder)
    {
        return builder.AddKonzole(konzoleBuilder => 
            konzoleBuilder.AddConsoleSink(options =>
            {
                options.UseColors = true;
                options.UseEmojis = true;
                options.PrettyPrint = true;
            })
        );
    }
    
    public static ILoggingBuilder AddKonzoleFile(this ILoggingBuilder builder, string? filePath = null)
    {
        return builder.AddKonzole(konzoleBuilder => 
            konzoleBuilder.AddFileSink(filePath)
        );
    }
    
    public static ILoggingBuilder AddKonzoleRemote(this ILoggingBuilder builder, string endpoint, string? apiKey = null)
    {
        return builder.AddKonzole(konzoleBuilder => 
            konzoleBuilder.AddRemoteSink(endpoint, apiKey)
        );
    }
    
    public static ILoggingBuilder AddKonzoleAll(this ILoggingBuilder builder, 
        string? filePath = null, 
        string? remoteEndpoint = null, 
        string? remoteApiKey = null)
    {
        return builder.AddKonzole(konzoleBuilder =>
        {
            konzoleBuilder.AddConsoleSink();
            
            if (!string.IsNullOrEmpty(filePath))
            {
                konzoleBuilder.AddFileSink(filePath);
            }
            
            if (!string.IsNullOrEmpty(remoteEndpoint))
            {
                konzoleBuilder.AddRemoteSink(remoteEndpoint, remoteApiKey);
            }
        });
    }
} 