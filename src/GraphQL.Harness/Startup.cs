using Example;
using GraphQL.Instrumentation;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars;
using GraphQL.SystemTextJson;
using Microsoft.Extensions.Options;

namespace GraphQL.Harness
{
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
                .AddErrorInfoProvider((opts, serviceProvider) =>
                {
                    var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
                    opts.ExposeExceptionStackTrace = settings.Value.ExposeExceptions;
                })
                .AddSchema<StarWarsSchema>()
                .AddGraphTypes(typeof(StarWarsQuery).Assembly)
                .AddMiddleware<CountFieldMiddleware>(false) // do not auto-install middleware
                .AddMiddleware<InstrumentFieldsMiddleware>(false) // do not auto-install middleware
                .ConfigureSchema((schema, serviceProvider) =>
                {
                    // install middleware only when the custom EnableMetrics option is set
                    var settings = serviceProvider.GetRequiredService<IOptions<GraphQLSettings>>();
                    if (settings.Value.EnableMetrics)
                    {
                        var middlewares = serviceProvider.GetRequiredService<IEnumerable<IFieldMiddleware>>();
                        foreach (var middleware in middlewares)
                            schema.FieldMiddleware.Use(middleware);
                    }
                }));

            // add something like repository
            services.AddSingleton<StarWarsData>();

            // add infrastructure stuff
            services.AddHttpContextAccessor();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<GraphQLMiddleware>();

            // add options configuration
            services.Configure<GraphQLSettings>(Configuration);
            services.Configure<GraphQLSettings>(settings => settings.BuildUserContext = ctx => new GraphQLUserContext { User = ctx.User });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseMiddleware<GraphQLMiddleware>();
            app.UseGraphQLPlayground();
            app.UseGraphQLGraphiQL();
            app.UseGraphQLAltair();
            app.UseGraphQLVoyager();
        }
    }
}
