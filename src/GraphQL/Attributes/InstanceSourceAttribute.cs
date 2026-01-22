namespace GraphQL;

/// <summary>
/// Specifies how instances of the CLR type should be obtained during GraphQL field resolution.
/// This attribute can be applied to classes to control instance creation behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class InstanceSourceAttribute : GraphQLAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="InstanceSourceAttribute"/> with the specified instance source.
    /// </summary>
    /// <param name="instanceSource">The method to use for obtaining instances.</param>
    public InstanceSourceAttribute(InstanceSource instanceSource)
    {
        InstanceSource = instanceSource;
    }

    /// <summary>
    /// Gets the instance source method.
    /// </summary>
    public InstanceSource InstanceSource { get; }
}

/// <summary>
/// Specifies how instances of a CLR type should be obtained during GraphQL field resolution.
/// </summary>
public enum InstanceSource
{
    /// <summary>
    /// Get the instance from <see cref="IResolveFieldContext.Source"/>.
    /// </summary>
    ContextSource = 0,

    /// <summary>
    /// Get the service from the service provider (<see cref="IResolveFieldContext.RequestServices"/>
    /// or the schema's configured service provider). If the service is not registered, create a new
    /// instance using the CLR type's constructor and dependency injection for constructor parameters and
    /// public required properties. Parameters or properties of type <see cref="IResolveFieldContext"/>
    /// are populated from the resolving field's current context. Similar to ASP.NET MVC controller instantiation.
    /// </summary>
    GetServiceOrCreateInstance = 1,

    /// <summary>
    /// Get the required service from the service provider (<see cref="IResolveFieldContext.RequestServices"/>
    /// or the schema's configured service provider). Throws an exception if the service is not registered.
    /// </summary>
    GetRequiredService = 2,

    /// <summary>
    /// Create a new instance using the CLR type's constructor and dependency injection for constructor parameters
    /// and public required properties. Parameters or properties of type <see cref="IResolveFieldContext"/>
    /// are populated from the resolving field's current context.
    /// </summary>
    NewInstance = 3,
}
