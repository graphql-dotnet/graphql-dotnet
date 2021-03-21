using System;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
    /// <summary>
    /// An <see cref="IServiceProvider"/> wrapper which instantiates classes that are unregistered in the
    /// underlying service provider. Intended to be passed to the <see cref="Schema"/> constructor so that
    /// all of the graph types do not individually need to be registered within your DI container.
    /// <br/><br/>
    /// To use this, simply register the <see cref="ISchema"/> itself as follows:
    /// <br/><br/>
    /// <code>services.AddSingleton&lt;ISchema, MySchema&gt;(services => new MySchema(new SelfActivatingServiceProvider(services)));</code>
    /// <br/><br/>
    /// Within your <see cref="Schema"/> constructor, you may need to set your <see cref="Schema.Query"/>, <see cref="Schema.Mutation"/> and
    /// <see cref="Schema.Subscription"/> fields to pull from the <see cref="SelfActivatingServiceProvider"/>:
    /// <br/><br/>
    /// <code>Query = services.GetRequiredService&lt;MyQuery&gt;();</code>
    /// <br/><br/>
    /// None of the graph types will need to be registered. Note that if any of the graph types implement
    /// IDisposable, be sure to register those types with your dependency injection provider, or their Dispose
    /// methods will not be called. Any dependencies of graph types that implement IDisposable will be
    /// disposed of properly, regardless of whether the graph type is registered within the service provider.
    /// </summary>
    public class SelfActivatingServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance with the specified underlying service provider.
        /// </summary>
        public SelfActivatingServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc cref="ActivatorUtilities.GetServiceOrCreateInstance(IServiceProvider, Type)"/>
        public object GetService(Type serviceType) => ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, serviceType);
    }
}
