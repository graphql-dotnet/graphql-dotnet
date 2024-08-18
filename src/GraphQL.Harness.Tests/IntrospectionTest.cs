using GraphQL.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Harness.Tests;

public class IntrospectionTest
{
    [Fact]
    public void VerifyIntrospection()
    {
        var services = new ServiceCollection();
        var startup = new Startup(new ConfigurationBuilder().Build());
        startup.ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print(new() { StringComparison = StringComparison.OrdinalIgnoreCase });

        sdl.ShouldMatchApproved(o => o.NoDiff());
    }
}
