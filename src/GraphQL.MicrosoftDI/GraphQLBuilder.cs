using System.Collections;
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
    public class GraphQLBuilder : GraphQLBuilderBase, IServiceCollection, IServiceRegister
    {
        /// <inheritdoc />
        public override IServiceRegister Services => this;

        /// <summary>
        /// Returns the underlying <see cref="IServiceCollection"/> of this builder.
        /// </summary>
        public IServiceCollection ServiceCollection { get; }

        /// <summary>
        /// Initializes a new instance for the specified service collection.
        /// </summary>
        /// <remarks>
        /// Registers various default services via <see cref="GraphQLBuilderBase.RegisterDefaultServices"/>
        /// after executing the configuration delegate.
        /// </remarks>
        public GraphQLBuilder(IServiceCollection services, Action<IGraphQLBuilder>? configure)
        {
            ServiceCollection = services ?? throw new ArgumentNullException(nameof(services));
            configure?.Invoke(this);
            RegisterDefaultServices();
        }

        /// <inheritdoc/>
        protected override void RegisterDefaultServices()
        {
            ServiceCollection.AddOptions();
            base.RegisterDefaultServices();
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
        public IServiceRegister Configure<TOptions>(Action<TOptions, IServiceProvider>? action = null)
            where TOptions : class, new()
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
        public IServiceRegister Register(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            if (replace)
            {
                ServiceCollection.Replace(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            }
            else
            {
                ServiceCollection.Add(new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime)));
            }
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegister Register(Type serviceType, Type implementationType, ServiceLifetime serviceLifetime, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            if (replace)
            {
                ServiceCollection.Replace(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            }
            else
            {
                ServiceCollection.Add(new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime)));
            }
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = false)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            if (replace)
            {
                ServiceCollection.Replace(new ServiceDescriptor(serviceType, implementationInstance));
            }
            else
            {
                ServiceCollection.Add(new ServiceDescriptor(serviceType, implementationInstance));
            }
            return this;
        }

        /// <inheritdoc/>
        public IServiceRegister TryRegister(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var descriptor = new ServiceDescriptor(serviceType, implementationFactory, TranslateLifetime(serviceLifetime));
            if (mode == RegistrationCompareMode.ServiceType)
                ServiceCollection.TryAdd(descriptor);
            else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
                ServiceCollection.TryAddEnumerable(descriptor);
            else
                throw new ArgumentOutOfRangeException(nameof(mode));

            return this;
        }

        /// <inheritdoc/>
        public IServiceRegister TryRegister(Type serviceType, Type implementationType, DI.ServiceLifetime serviceLifetime, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            var descriptor = new ServiceDescriptor(serviceType, implementationType, TranslateLifetime(serviceLifetime));
            if (mode == RegistrationCompareMode.ServiceType)
                ServiceCollection.TryAdd(descriptor);
            else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
                ServiceCollection.TryAddEnumerable(descriptor);
            else
                throw new ArgumentOutOfRangeException(nameof(mode));

            return this;
        }

        /// <inheritdoc/>
        public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationInstance == null)
                throw new ArgumentNullException(nameof(implementationInstance));

            var descriptor = new ServiceDescriptor(serviceType, implementationInstance);
            if (mode == RegistrationCompareMode.ServiceType)
                ServiceCollection.TryAdd(descriptor);
            else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
                ServiceCollection.TryAddEnumerable(descriptor);
            else
                throw new ArgumentOutOfRangeException(nameof(mode));

            return this;
        }

        int ICollection<ServiceDescriptor>.Count => ServiceCollection.Count;
        bool ICollection<ServiceDescriptor>.IsReadOnly => ServiceCollection.IsReadOnly;
        ServiceDescriptor IList<ServiceDescriptor>.this[int index] { get => ServiceCollection[index]; set => ServiceCollection[index] = value; }
        int IList<ServiceDescriptor>.IndexOf(ServiceDescriptor item) => ServiceCollection.IndexOf(item);
        void IList<ServiceDescriptor>.Insert(int index, ServiceDescriptor item) => ServiceCollection.Insert(index, item);
        void IList<ServiceDescriptor>.RemoveAt(int index) => ServiceCollection.RemoveAt(index);
        void ICollection<ServiceDescriptor>.Add(ServiceDescriptor item) => ServiceCollection.Add(item);
        void ICollection<ServiceDescriptor>.Clear() => ServiceCollection.Clear();
        bool ICollection<ServiceDescriptor>.Contains(ServiceDescriptor item) => ServiceCollection.Contains(item);
        void ICollection<ServiceDescriptor>.CopyTo(ServiceDescriptor[] array, int arrayIndex) => ServiceCollection.CopyTo(array, arrayIndex);
        bool ICollection<ServiceDescriptor>.Remove(ServiceDescriptor item) => ServiceCollection.Remove(item);
        IEnumerator<ServiceDescriptor> IEnumerable<ServiceDescriptor>.GetEnumerator() => ServiceCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)ServiceCollection).GetEnumerator();
    }
}
