#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
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
    public class GraphQLBuilder : GraphQLBuilderBase, IServiceCollection
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
        public GraphQLBuilder(IServiceCollection services, Action<IGraphQLBuilder> configure)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            services.AddOptions();
            configure?.Invoke(this);
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
            this.TryRegister(services => services.GetService<IOptions<TOptions>>()?.Value ?? new TOptions(), ServiceLifetime.Singleton);
            if (action != null)
            {
                // This is used instead of "normal" services.Configure(configureOptions) to pass IServiceProvider to user code.
                this.Register<IConfigureOptions<TOptions>>(services => new ConfigureNamedOptions<TOptions>(Options.DefaultName, opt => action(opt, services)), ServiceLifetime.Singleton);
            }

            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            if (replace)
            {
                Services.Replace(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            }
            else
            {
                Services.Add(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            }
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            if (replace)
            {
                Services.Replace(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            }
            else
            {
                Services.Add(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            }
            return this;
        }

        /// <inheritdoc/>
        public override IGraphQLBuilder Register(Type serviceType, object implementationInstance, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            if (replace)
            {
                Services.Replace(new ServiceDescriptor(serviceType, implementationInstance));
            }
            else
            {
                Services.Add(new ServiceDescriptor(serviceType, implementationInstance));
            }
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

        int ICollection<ServiceDescriptor>.Count => Services.Count;
        bool ICollection<ServiceDescriptor>.IsReadOnly => Services.IsReadOnly;
        ServiceDescriptor IList<ServiceDescriptor>.this[int index] { get => Services[index]; set => Services[index] = value; }
        int IList<ServiceDescriptor>.IndexOf(ServiceDescriptor item) => Services.IndexOf(item);
        void IList<ServiceDescriptor>.Insert(int index, ServiceDescriptor item) => Services.Insert(index, item);
        void IList<ServiceDescriptor>.RemoveAt(int index) => Services.RemoveAt(index);
        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => Services.Add(item);
        void ICollection<ServiceDescriptor>.Clear() => Services.Clear();
        bool ICollection<ServiceDescriptor>.Contains(ServiceDescriptor item) => Services.Contains(item);
        void ICollection<ServiceDescriptor>.CopyTo(ServiceDescriptor[] array, int arrayIndex) => Services.CopyTo(array, arrayIndex);
        bool ICollection<ServiceDescriptor>.Remove(ServiceDescriptor item) => Services.Remove(item);
        IEnumerator<ServiceDescriptor> IEnumerable<ServiceDescriptor>.GetEnumerator() => Services.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Services).GetEnumerator();
    }
}
