using GraphQL.Analyzers.SourceGenerators.Models;

namespace GraphQL.Analyzers.Interceptors.Models;

/// <summary>
/// Represents a single parameter of the delegate method being intercepted.
/// </summary>
internal sealed record DelegateParameterInfo
{
    /// <summary>
    /// The fully qualified type name of the parameter.
    /// </summary>
    public required string FullyQualifiedTypeName { get; init; }

    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public required string ParameterName { get; init; }

    /// <summary>
    /// Whether this parameter is a special parameter resolved from context
    /// (e.g. IResolveFieldContext, CancellationToken) rather than via BuildArgument.
    /// </summary>
    public bool IsContextParameter { get; init; }
}

/// <summary>
/// Contains primitive information about a FieldBuilder.ResolveDelegate call that should be intercepted.
/// This record holds all data needed to generate an AOT-compatible resolver.
/// </summary>
internal sealed record ResolveDelegateInterceptorInfo
{
    /// <summary>
    /// The interceptable location of the ResolveDelegate method call.
    /// </summary>
    public required ComparableInterceptableLocation Location { get; init; }

    /// <summary>
    /// The fully qualified name of the source type (TSourceType from FieldBuilder&lt;TSourceType, TReturnType&gt;).
    /// </summary>
    public required string SourceTypeFullName { get; init; }

    /// <summary>
    /// The fully qualified name of the return type (TReturnType from FieldBuilder&lt;TSourceType, TReturnType&gt;).
    /// </summary>
    public required string ReturnTypeFullName { get; init; }

    /// <summary>
    /// The fully qualified name of the type that declares the delegate method.
    /// </summary>
    public required string DeclaringTypeFullName { get; init; }

    /// <summary>
    /// The name of the method being delegated to.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// The parameters of the delegate method.
    /// </summary>
    public required ImmutableEquatableArray<DelegateParameterInfo> Parameters { get; init; }

    /// <summary>
    /// Whether the delegate target is a static method.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Whether the delegate is null (i.e. ResolveDelegate(null) was called).
    /// </summary>
    public bool IsNullDelegate { get; init; }

    /// <summary>
    /// The fully qualified type names of the method parameters for disambiguation (overload resolution).
    /// </summary>
    public required ImmutableEquatableArray<string> MethodParameterTypeNames { get; init; }
}
