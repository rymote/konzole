using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rymote.Konzole.Configuration;
using Rymote.Konzole.Extensions;
using Xunit;

namespace Rymote.Konzole.Tests.Configuration;

public class KonzoleBuilderTests
{
    [Fact]
    public void Build_Throws_WhenNoSinksConfigured()
    {
        ServiceCollection services = new();
        services.AddLogging();

        ILoggingBuilder loggingBuilder = new TestLoggingBuilder(services);
        KonzoleBuilder konzoleBuilder = new(loggingBuilder);

        Assert.Throws<InvalidOperationException>(() => konzoleBuilder.Build());
    }

    [Fact]
    public void Build_RegistersHttpClientFactory_WhenHttpSinkUsed()
    {
        ServiceCollection services = new();
        services.AddLogging(builder =>
        {
            builder.AddKonzole(konzole => konzole.AddRemoteSink("https://example.test/log"));
        });

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<System.Net.Http.IHttpClientFactory>());
    }

    private sealed class TestLoggingBuilder : ILoggingBuilder
    {
        public TestLoggingBuilder(IServiceCollection services) { Services = services; }
        public IServiceCollection Services { get; }
    }
}
