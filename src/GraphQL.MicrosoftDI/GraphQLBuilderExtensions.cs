using System;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphQL.MicrosoftDI
{
    public static class GraphQLBuilderExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
            => new GraphQLBuilder(services); // call AddOptions here? probably the job of the caller.

        public static IGraphQLBuilder AddSelfActivatingSchema<TSchema>(this IGraphQLBuilder builder, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TSchema : class, ISchema
        {
            if (serviceLifetime == ServiceLifetime.Transient && typeof(IDisposable).IsAssignableFrom(typeof(TSchema)))
            {
                // This scenario can cause a memory leak if the schema is requested from the root service provider.
                // If it was requested from a scoped provider, then there is no reason to register it as transient.
                // See following link:
                // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#disposable-transient-services-captured-by-container
                throw new InvalidOperationException("A schema that implements IDisposable cannot be registered as a transient service.");
            }
            
            // Register the service with the DI provider as TSchema, overwriting any existing registration
            builder.Register(serviceLifetime, services =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(services);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            });

            // Now register the service as ISchema if not already registered.
            builder.TryRegister<ISchema>(serviceLifetime, services =>
            {
                var selfActivatingServices = new SelfActivatingServiceProvider(services);
                var schema = ActivatorUtilities.CreateInstance<TSchema>(selfActivatingServices);
                return schema;
            });

            return builder;
        }

        public static IGraphQLBuilder ConfigureErrorInfoProvider(this IGraphQLBuilder builder, Action<ErrorInfoProviderOptions, IServiceProvider> configureOptions)
        {
            builder.Register<IConfigureOptions<ErrorInfoProviderOptions>>(ServiceLifetime.Singleton, x => new ConfigureNamedOptions<ErrorInfoProviderOptions>(Options.DefaultName, opt => configureOptions(opt, x)));
            return builder;
        }

        public static IGraphQLBuilder ConfigureComplexity(this IGraphQLBuilder builder, Action<ComplexityConfiguration, IServiceProvider> configureOptions)
        {
            builder.Register<IConfigureOptions<ComplexityConfiguration>>(ServiceLifetime.Singleton, x => new ConfigureNamedOptions<ComplexityConfiguration>(Options.DefaultName, opt => configureOptions(opt, x)));
            return builder;
        }
    }
}
