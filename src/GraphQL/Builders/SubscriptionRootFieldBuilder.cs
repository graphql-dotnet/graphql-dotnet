using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Builders;

/// <summary>
/// Builds a root field for subscription graph type with a specified source type and return type.
/// </summary>
/// <typeparam name="TSourceType">The type of <see cref="IResolveFieldContext.Source"/>.</typeparam>
/// <typeparam name="TReturnType">The type of the return value of the resolver.</typeparam>
public class SubscriptionRootFieldBuilder<TSourceType, TReturnType> : ObjectFieldBuilder<TSourceType, TReturnType>
{
    /// <summary>
    /// Initializes a new instance for the specified <see cref="SubscriptionRootFieldType"/>.
    /// </summary>
    protected SubscriptionRootFieldBuilder(SubscriptionRootFieldType fieldType)
        : base(fieldType)
    {
    }

    /// <summary>
    /// Returns a builder for a new field.
    /// </summary>
    /// <param name="type">The graph type of the field.</param>
    /// <param name="name">The name of the field.</param>
    public static new SubscriptionRootFieldBuilder<TSourceType, TReturnType> Create(IGraphType type, string name = "default")
    {
        var fieldType = new SubscriptionRootFieldType
        {
            Name = name,
            ResolvedType = type,
        };
        return new SubscriptionRootFieldBuilder<TSourceType, TReturnType>(fieldType);
    }

    /// <inheritdoc cref="Create(IGraphType, string)"/>
    public static new SubscriptionRootFieldBuilder<TSourceType, TReturnType> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? type = null, string name = "default")
    {
        var fieldType = new SubscriptionRootFieldType
        {
            Name = name,
            Type = type,
        };
        return new SubscriptionRootFieldBuilder<TSourceType, TReturnType>(fieldType);
    }

    /// <summary>
    /// Sets a source stream resolver for the field.
    /// </summary>
    public virtual SubscriptionRootFieldBuilder<TSourceType, TReturnType> ResolveStream(Func<IResolveFieldContext<TSourceType>, IObservable<TReturnType?>> sourceStreamResolver)
    {
        ((SubscriptionRootFieldType)FieldType).StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(sourceStreamResolver);
        FieldType.Resolver ??= SourceFieldResolver.Instance;
        return this;
    }

    /// <summary>
    /// Sets a source stream resolver for the field.
    /// </summary>
    public virtual SubscriptionRootFieldBuilder<TSourceType, TReturnType> ResolveStreamAsync(Func<IResolveFieldContext<TSourceType>, Task<IObservable<TReturnType?>>> sourceStreamResolver)
    {
        ((SubscriptionRootFieldType)FieldType).StreamResolver = new SourceStreamResolver<TSourceType, TReturnType>(context => new ValueTask<IObservable<TReturnType?>>(sourceStreamResolver(context)));
        FieldType.Resolver ??= SourceFieldResolver.Instance;
        return this;
    }
}
