using System.Collections.Generic;
using Example;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.MicrosoftDI;
using GraphQL.StarWars;
using GraphQL.SystemTextJson;
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
                .AddDocumentWriter()
                .AddSchema<StarWarsSchema>()
                .AddErrorInfoProvider(services =>
                {
                    var settings = services.GetRequiredService<IOptions<GraphQLSettings>>();
                    return new ErrorInfoProviderOptions { ExposeExceptionStackTrace = settings.Value.ExposeExceptions };
                })
                .AddGraphTypes(typeof(StarWarsQuery).Assembly)
                .AddSchema(services =>
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

            // add something like repository
            services.AddSingleton<StarWarsData>();

            // add infrastructure stuff
            services.AddHttpContextAccessor();
            services.AddLogging(builder => builder.AddConsole());

            // add options configuration
            services.Configure<GraphQLSettings>(Configuration);
            services.Configure<GraphQLSettings>(settings => settings.BuildUserContext = ctx => new GraphQLUserContext { User = ctx.User });

            // add Field Middlewares
            services.AddSingleton<IFieldMiddleware, CountFieldMiddleware>();
            services.AddSingleton<IFieldMiddleware, InstrumentFieldsMiddleware>();
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
