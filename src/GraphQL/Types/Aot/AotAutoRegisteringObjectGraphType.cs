using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Types.Aot;

/// <summary>
/// Provides a base class for registering object graph types in AOT (Ahead-Of-Time) compiled environments.
/// </summary>
public abstract class AotAutoRegisteringObjectGraphType<TSource> : AutoRegisteringObjectGraphType<TSource>
{
    /// <inheritdoc/>
    public AotAutoRegisteringObjectGraphType() : base(true)
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<MemberInfo> GetRegisteredMembers() => throw new NotImplementedException("GetRegisteredMembers must be implemented by the derived class if used.");
    /// <inheritdoc/>
    protected override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => throw new NotImplementedException("BuildFieldType must be implemented by the derived class if used.");

    /// <summary>
    /// Returns the instance of <typeparamref name="TSource"/> for a given field resolution.
    /// </summary>
    protected virtual TSource GetMemberInstance(IResolveFieldContext context) => (TSource)(context.Source ?? AutoRegisteringOutputHelper.ThrowSourceNullException());
    /// <inheritdoc/>
    protected sealed override LambdaExpression BuildMemberInstanceExpression(MemberInfo memberInfo) => throw new NotSupportedException();

    /// <summary>
    /// Builds a field argument and returns a resolver.
    /// </summary>
    protected virtual Func<IResolveFieldContext, TParameterType> BuildArgument<TParameterType>(FieldType fieldType, ParameterInfo parameterInfo)
    {
        var argumentInfo = GetArgumentInformation(fieldType, parameterInfo);
        var queryArgument = argumentInfo.ConstructQueryArgument();
        ApplyArgumentAttributes(parameterInfo, queryArgument);
        var resolver = GetParameterResolver<TParameterType>(argumentInfo);
        if (resolver == null)
        {
            (fieldType.Arguments ??= new()).Add(queryArgument);
            return context => context.GetArgument<TParameterType>(queryArgument.Name);
        }
        return resolver;
    }

    /// <summary>
    /// Builds a field resolver using the provided function.
    /// </summary>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, T> fn, bool requiresAccessor)
        => requiresAccessor
        ? new FuncFieldResolver<T>(context => fn(context))
        : new FuncFieldResolverNoAccessor<T>(context => fn(context));

    /// <inheritdoc cref="BuildFieldResolver{T}(Func{IResolveFieldContext, T}, bool)"/>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, Task<T>> fn, bool requiresAccessor)
        => requiresAccessor
        ? new FuncFieldResolver<T>(context => new ValueTask<T>(fn(context))!)
        : new FuncFieldResolverNoAccessor<T>(context => new ValueTask<T>(fn(context))!);

    /// <inheritdoc cref="BuildFieldResolver{T}(Func{IResolveFieldContext, T}, bool)"/>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, ValueTask<T>> fn, bool requiresAccessor)
        => requiresAccessor
        ? new FuncFieldResolver<T>(fn!)
        : new FuncFieldResolverNoAccessor<T>(fn!);
}
