using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;

namespace Rymote.Konzole.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder, Action<KonzoleBuilder> configure)
    {
        KonzoleBuilder konzoleBuilder = new(builder);
        configure(konzoleBuilder);
        konzoleBuilder.Build();
        return builder;
    }

    public static ILoggingBuilder AddKonzole(this ILoggingBuilder builder) =>
        builder.AddKonzole(konzoleBuilder => konzoleBuilder.AddConsoleSink(options =>
        {
            options.UseColors = true;
            options.UseEmojis = true;
        }));
}
