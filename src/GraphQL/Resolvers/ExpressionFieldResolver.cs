using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers;

/// <summary>
/// Returns a value from the field's graph type's source object, based on a predefined expression.
/// <br/><br/>
/// Supports asynchronous return types.
/// <br/><br/>
/// Note: this class uses dynamic compilation and therefore allocates a relatively large amount of
/// memory in managed heap, ~1KB. Do not use this class in cases with limited memory requirements.
/// </summary>
public class ExpressionFieldResolver<TSourceType, TProperty> : IFieldResolver, IRequiresResolveFieldContextAccessor
{
    private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;

    /// <summary>
    /// Initializes a new instance that runs the specified expression when resolving a field.
    /// <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/> return types are also supported.
    /// </summary>
    [RequiresDynamicCode("Calls MemberResolver.BuildFieldResolverInternal which calls a generic method and compiles a lambda at runtime.")]
    public ExpressionFieldResolver(Expression<Func<TSourceType, TProperty>> property)
    {
        var param = Expression.Parameter(typeof(IResolveFieldContext), "context");
        var source = Expression.MakeMemberAccess(param, _sourcePropertyInfo);
        var cast = Expression.Convert(source, typeof(TSourceType));
        var body = property.Body.Replace(property.Parameters[0], cast);
        _resolver = MemberResolver.BuildFieldResolverInternal(param, body);

        // require the context accessor unless the expression is a simple member access to a property or field
        RequiresResolveFieldContextAccessor = !(property.Body is MemberExpression { Member: PropertyInfo or FieldInfo });
    }

    private static readonly PropertyInfo _sourcePropertyInfo = typeof(IResolveFieldContext).GetProperty(nameof(IResolveFieldContext.Source))!;

    /// <inheritdoc/>
    public bool RequiresResolveFieldContextAccessor { get; }

    /// <inheritdoc/>
    ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
        => _resolver(context);
}
