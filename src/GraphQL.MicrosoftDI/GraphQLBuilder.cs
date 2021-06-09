using System;
using GraphQL.Execution;
using GraphQL.Validation.Complexity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.MicrosoftDI
{
    public class GraphQLBuilder : GraphQLBuilderBase
    {
        private readonly IServiceCollection _services;
        public GraphQLBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            Initialize();

            // configure mapping for IOptions<ComplexityConfiguation> and IOptions<ErrorInfoProviderOptions>
            // note that this code will cause a null to be passed into applicable constructor arguments during DI injection if these objects are unconfigured
            TryRegister(ServiceLifetime.Transient, services => services.GetService<IOptions<ComplexityConfiguration>>()?.Value); // Registering IOptions<ComplexityConfiguration> or registering ComplexityConfiguration will work
            TryRegister(ServiceLifetime.Transient, services => services.GetService<IOptions<ErrorInfoProviderOptions>>()?.Value); // Registering IOptions<ErrorInfoProviderOptions> or registering ErrorInfoProviderOptions will work
        }

        public override IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.AddSingleton(implementationFactory);
                    break;
                case ServiceLifetime.Scoped:
                    _services.AddScoped(implementationFactory);
                    break;
                case ServiceLifetime.Transient:
                    _services.AddTransient(implementationFactory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime));
            }
            return this;
        }

        public override IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.AddSingleton(serviceType, implementationType);
                    break;
                case ServiceLifetime.Scoped:
                    _services.AddScoped(serviceType, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    _services.AddTransient(serviceType, implementationType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime));
            }
            return this;
        }

        public override IGraphQLBuilder TryRegister<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.TryAddSingleton(implementationFactory);
                    break;
                case ServiceLifetime.Scoped:
                    _services.TryAddScoped(implementationFactory);
                    break;
                case ServiceLifetime.Transient:
                    _services.TryAddTransient(implementationFactory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime));
            }
            return this;
        }

        public override IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            switch (serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    _services.TryAddSingleton(serviceType, implementationType);
                    break;
                case ServiceLifetime.Scoped:
                    _services.TryAddScoped(serviceType, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    _services.TryAddTransient(serviceType, implementationType);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime));
            }
            return this;
        }
    }
}
