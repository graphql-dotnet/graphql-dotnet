using System;
using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// An implementation of <see cref="IGraphQLBuilder"/> which uses the Microsoft dependency injection framework
    /// to register services and configure options.
    /// </summary>
    internal class GraphQLBuilder : GraphQLBuilderBase
    {
        private readonly IServiceCollection _services;
        /// <summary>
        /// Initializes a new instance for the specified service collection.
        /// </summary>
        /// <remarks>
        /// Registers various default services via <see cref="GraphQLBuilderBase.Initialize"/>.
        /// </remarks>
        public GraphQLBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            services.AddOptions();
            Initialize();
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider> action = null)
        {
            TryRegister(services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions(), ServiceLifetime.Singleton);
            if (action != null)
            {
                Register<IConfigureOptions<TOptions>>(services => new ConfigureNamedOptions<TOptions>(Options.DefaultName, opt => action(opt, services)), ServiceLifetime.Singleton);
            }

            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register<TService>(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override IGraphQLBuilder TryRegister<TService>(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime serviceLifetime)
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

        /// <inheritdoc/>
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
