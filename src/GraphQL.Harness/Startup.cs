using System.Collections.Generic;
using Example;
using GraphQL.Instrumentation;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars;
using GraphQL.StarWars.DataRepository;
using GraphQL.StarWars.Types;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            services.AddGraphQL()
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
                });

            // register static data
            services.AddSingleton<StarWarsData>();

            // register repository
            services.AddScoped<IStarWarsDataRespository, StarWarsDataRespository>();

            // add graph types
            services.AddSingleton<StarWarsQuery>();
            services.AddSingleton<StarWarsMutation>();
            services.AddSingleton<HumanType>();
            services.AddSingleton<HumanInputType>();
            services.AddSingleton<DroidType>();
            services.AddSingleton<CharacterInterface>();
            services.AddSingleton<EpisodeEnum>();

            // add schema
            services.AddSingleton<ISchema, StarWarsSchema>(services =>
            {
                var settings = services.GetRequiredService<IOptions<GraphQLSettings>>();
                var schema = new StarWarsSchema(services);
                if (settings.Value.EnableMetrics)
                {
                    var middlewares = services.GetRequiredService<IEnumerable<IFieldMiddleware>>();
                    foreach (var middleware in middlewares)
                        schema.FieldMiddleware.Use(middleware);
                }
                return schema;
            });

            // add infrastructure stuff
            services.AddHttpContextAccessor();
            services.AddLogging(builder => builder.AddConsole());

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
