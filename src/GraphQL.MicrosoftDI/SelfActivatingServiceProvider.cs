using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

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
/// <see cref="IDisposable"/>, be sure to register those types with your dependency injection provider,
/// or their <see cref="IDisposable.Dispose"/> methods will not be called. Any dependencies of graph types
/// that implement <see cref="IDisposable"/> will be disposed of properly, regardless of whether the graph
/// type is registered within the service provider.
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
    public object GetService(Type serviceType)
    {
        // if the type is an interface, attempt to retrieve the interface registration from the
        // underlying service provider or else return null. (May trigger an exception in a method
        // calling GetRequiredService, of course.) But for concrete classes, attempt to
        // create the instance if it is not able to be returned from the service provider.
        // Note: this does not intelligently choose the constructor but rather tries the first one it encounters.
        if (serviceType == typeof(IServiceProvider) || serviceType == typeof(SelfActivatingServiceProvider))
            return this;
        else if (!serviceType.IsAbstract && serviceType.IsClass && !serviceType.IsGenericTypeDefinition)
            return ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, serviceType);
        else
            return _serviceProvider.GetService(serviceType);
    }
}
