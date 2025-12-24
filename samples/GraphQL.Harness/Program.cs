#if !NET10_0_OR_GREATER
using Microsoft.AspNetCore;
#endif

namespace GraphQL.Harness;

public static class Program
{
    public static void Main(string[] args) => BuildWebHost(args).Run();

#if NET10_0_OR_GREATER
    public static IHost BuildWebHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var startup = new Startup(builder.Configuration);

        // Call ConfigureServices
        startup.ConfigureServices(builder.Services);

        var app = builder.Build();

        // Call Configure
        startup.Configure(app, app.Environment);

        return app;
    }
#else
    public static IWebHost BuildWebHost(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build();
    }
#endif
}
