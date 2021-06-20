using System;
using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL.MicrosoftDI
{
    public class GraphQLBuilder : GraphQLBuilderBase
    {
        private readonly IServiceCollection _services;
        public GraphQLBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            Initialize();
        }

        public override IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
        {
            TryRegister(ServiceLifetime.Singleton, services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions());
            if (action != null)
            {
                Register<IPostConfigureOptions<TOptions>>(ServiceLifetime.Singleton, services => new PostConfigureOptions<TOptions>(Options.DefaultName, opt => action(opt, services)));
            }

            return this;
        }

        public override IGraphQLBuilder ConfigureDefaults<TOptions>(Action<TOptions, IServiceProvider> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            TryRegister(ServiceLifetime.Singleton, services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions());
            Register<IConfigureOptions<TOptions>>(ServiceLifetime.Singleton, services => new ConfigureNamedOptions<TOptions>(Options.DefaultName, opt => action(opt, services)));

            return this;
        }

        public override IGraphQLBuilder Register<TService>(ServiceLifetime serviceLifetime, Func<IServiceProvider, TService> implementationFactory)
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
