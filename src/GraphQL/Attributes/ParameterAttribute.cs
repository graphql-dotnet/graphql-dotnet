using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// Base class for attributes that modify method parameters by providing a custom value resolver.
/// This attribute does not inherit from <see cref="GraphQLAttribute"/> and is used exclusively
/// for parameter modification scenarios.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public abstract class ParameterAttribute : Attribute
{
    /// <summary>
    /// Returns a delegate that resolves the parameter value from the field context.
    /// </summary>
    /// <typeparam name="T">The CLR type of the method parameter.</typeparam>
    /// <param name="argumentInformation">The <see cref="ArgumentInformation"/> for the parameter.</param>
    /// <returns>A function that takes an <see cref="IResolveFieldContext"/> and returns the parameter value of type <typeparamref name="T"/>.</returns>
    public abstract Func<IResolveFieldContext, T> GetResolver<T>(ArgumentInformation argumentInformation);
}

/// <inheritdoc/>
[Obsolete("This class for internal usage by GraphQL.NET. Derive from ParameterAttribute or ParameterAttribute<T>.")]
public abstract class TypedParameterAttribute : ParameterAttribute
{
    /// <summary>
    /// Returns the parameter type that this attribute applies to
    /// </summary>
    internal abstract Type ParameterType { get; }

    /// <inheritdoc cref="ParameterAttribute.GetResolver{T}(ArgumentInformation)"/>
    internal abstract Delegate GetResolverDelegate(ArgumentInformation argumentInformation);
}

/// <inheritdoc/>
#pragma warning disable CS0618 // Type or member is obsolete
public abstract class ParameterAttribute<TParameterType> : TypedParameterAttribute
#pragma warning restore CS0618 // Type or member is obsolete
{
    /// <summary>
    /// Returns the parameter type that this attribute applies to
    /// </summary>
    internal sealed override Type ParameterType => typeof(TParameterType);

    /// <inheritdoc cref="ParameterAttribute.GetResolver{T}(ArgumentInformation)"/>
    public abstract Func<IResolveFieldContext, TParameterType> GetResolver(ArgumentInformation argumentInformation);

    /// <inheritdoc/>
    public sealed override Func<IResolveFieldContext, T> GetResolver<T>(ArgumentInformation argumentInformation)
    {
        if (typeof(T) == typeof(TParameterType))
        {
            return (Func<IResolveFieldContext, T>)(Delegate)GetResolver(argumentInformation);
        }
        throw new InvalidOperationException($"Type mismatch in {GetType().GetFriendlyName()}: requested type '{typeof(T).GetFriendlyName()}' does not match the parameter attribute's type '{typeof(TParameterType).GetFriendlyName()}'.");
    }

    /// <inheritdoc cref="ParameterAttribute.GetResolver{T}(ArgumentInformation)"/>
    internal sealed override Delegate GetResolverDelegate(ArgumentInformation argumentInformation)
    {
        return GetResolver(argumentInformation);
    }
}
