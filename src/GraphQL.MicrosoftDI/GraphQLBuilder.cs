#nullable enable

using System;
using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MSServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using ServiceLifetime = GraphQL.DI.ServiceLifetime;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// An implementation of <see cref="IGraphQLBuilder"/> which uses the Microsoft dependency injection framework
    /// to register services and configure options.
    /// </summary>
    public class GraphQLBuilder : GraphQLBuilderBase
    {
        /// <summary>
        /// Returns the underlying <see cref="IServiceCollection"/> of this builder.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Initializes a new instance for the specified service collection.
        /// </summary>
        /// <remarks>
        /// Registers various default services via <see cref="GraphQLBuilderBase.Initialize"/>.
        /// </remarks>
        public GraphQLBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            services.AddOptions();
            Initialize();
        }

        private static MSServiceLifetime TranslateLifetime(ServiceLifetime serviceLifetime)
            => serviceLifetime switch
            {
                ServiceLifetime.Singleton => MSServiceLifetime.Singleton,
                ServiceLifetime.Scoped => MSServiceLifetime.Scoped,
                ServiceLifetime.Transient => MSServiceLifetime.Transient,
                _ => throw new ArgumentOutOfRangeException(nameof(serviceLifetime))
            };

        /// <inheritdoc/>
        public override IGraphQLBuilder Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
        {
            this.TryRegister(services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions(), DI.ServiceLifetime.Singleton);
            if (action != null)
            {
                this.Register<IConfigureOptions<TOptions>>(services => new ConfigureNamedOptions<TOptions>(Options.DefaultName, opt => action(opt, services)), ServiceLifetime.Singleton);
            }

            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            Services.Add(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            Services.Add(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, object implementationInstance)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            Services.Add(new ServiceDescriptor(serviceType, implementationInstance));
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            Services.TryAdd(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder TryRegister(Type serviceType, Type implementationType, DI.ServiceLifetime serviceLifetime)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            Services.TryAdd(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder TryRegister(Type serviceType, object implementationInstance)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            Services.TryAdd(new ServiceDescriptor(serviceType, implementationInstance));
            return this;
        }
    }
}
