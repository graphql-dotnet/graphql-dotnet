using Example;
using GraphQL.Instrumentation;
using GraphQL.StarWars;
using GraphQL.Types;

namespace GraphQL.Harness;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // add execution components
        services.AddGraphQL(builder => builder
            .AddSystemTextJson()
            .AddSchema<StarWarsSchema>()
            .AddGraphTypes(typeof(StarWarsQuery).Assembly)
            .UseMiddleware<CountFieldMiddleware>(false) // do not auto-install middleware
            .UseMiddleware<InstrumentFieldsMiddleware>(false) // do not auto-install middleware
        );

        // add something like repository
        services.AddSingleton<StarWarsData>();

        // add infrastructure stuff
        services.AddHttpContextAccessor();
        services.AddLogging(builder => builder.AddConsole());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.ApplicationServices.GetRequiredService<ISchema>();
        app.ApplicationServices.GetRequiredService<IDocumentExecuter<ISchema>>();
        app.ApplicationServices.GetRequiredService<IGraphQLSerializer>();
        app.UseGraphQL();
        app.UseGraphQLPlayground();
        app.UseGraphQLGraphiQL();
        app.UseGraphQLAltair();
        app.UseGraphQLVoyager();
    }
}
