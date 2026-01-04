using GraphQL.StarWars.SchemaFirst;
using GraphQL.StarWars.SchemaFirst.Models;
using GraphQL.StarWars.SchemaFirst.Resolvers;
using GraphQL.Types;

namespace GraphQL.Harness.SchemaFirst;

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
            .AddSchema(BuildSchema)
            .ValidateServices()
        );

        // add data repository
        services.AddSingleton<StarWarsData>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseGraphQL();
        app.UseGraphQLGraphiQL();
    }

    private static ISchema BuildSchema(IServiceProvider serviceProvider)
    {
        // load the schema-first SDL from an embedded resource
        var filename = "GraphQL.StarWars.SchemaFirst.Schema.gql";
        var assembly = typeof(StarWarsData).Assembly;
        var stream = assembly.GetManifestResourceStream(filename)
            ?? throw new InvalidOperationException("Could not read schema definitions from embedded resource.");
        var reader = new StreamReader(stream);
        var schemaString = reader.ReadToEnd();

        // build the schema with configuration
        var schema = Schema.For(schemaString, builder =>
        {
            builder.ServiceProvider = serviceProvider;

            // Register resolver classes
            builder.Types.Include<Query>();
            builder.Types.Include<Mutation>();
            builder.Types.Include<Character>();

            // Register model types - both for field resolution and interface type resolution
            builder.Types.Include<Human>();
            builder.Types.Include<Droid>();
            builder.Types.For("Human").IsTypeOf<Human>();
            builder.Types.For("Droid").IsTypeOf<Droid>();
        });

        schema.Description = "Example StarWars universe schema (schema-first)";
        return schema;
    }
}
