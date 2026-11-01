using GraphQL.StarWars;
using GraphQL.Validation.Sample.ValidationRules;

namespace GraphQL.Validation.Sample;

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
        // add execution components with custom validation rules
        services.AddGraphQL(builder => builder
            .AddSystemTextJson()
            .AddSchema<StarWarsSchema>()
            .AddGraphTypes(typeof(StarWarsQuery).Assembly)
            // Add custom validation rules
            .AddValidationRule<NoIntrospectionValidationRule>()
            .AddValidationRule<InputFieldsOfCorrectLengthValidationRule>()
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

        app.UseGraphQL();
        app.UseGraphQLGraphiQL();
        app.UseGraphQLAltair();
        app.UseGraphQLVoyager();
    }
}
