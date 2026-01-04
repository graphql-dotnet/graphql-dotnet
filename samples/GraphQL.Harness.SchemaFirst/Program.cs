namespace GraphQL.Harness.SchemaFirst;

public static class Program
{
    public static void Main(string[] args) => BuildWebHost(args).Run();

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
}
