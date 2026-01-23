using System.Reflection;
using GraphQL.Resolvers;

namespace GraphQL.Types.Aot;

/// <summary>
/// Provides a base class for registering interface graph types in AOT (Ahead-Of-Time) compiled environments.
/// </summary>
public abstract class AotAutoRegisteringInterfaceGraphType<TSource> : AutoRegisteringInterfaceGraphType<TSource>
{
    /// <inheritdoc/>
    public AotAutoRegisteringInterfaceGraphType() : base(true)
    {
    }

    /// <inheritdoc/>
    protected sealed override IEnumerable<MemberInfo> GetRegisteredMembers() => throw new NotImplementedException("GetRegisteredMembers must be implemented by the derived class if used.");
    /// <inheritdoc/>
    protected sealed override void BuildFieldType(FieldType fieldType, MemberInfo memberInfo) => throw new NotImplementedException("BuildFieldType must be implemented by the derived class if used.");

    /// <summary>
    /// Builds a field resolver using the provided function.
    /// </summary>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, T> fn) => new FuncFieldResolver<T>(context => fn(context));
    /// <inheritdoc cref="BuildFieldResolver{T}(Func{IResolveFieldContext, T})"/>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, Task<T>> fn) => new FuncFieldResolver<T>(context => new ValueTask<T>(fn(context))!);
    /// <inheritdoc cref="BuildFieldResolver{T}(Func{IResolveFieldContext, T})"/>
    protected IFieldResolver BuildFieldResolver<T>(Func<IResolveFieldContext, ValueTask<T>> fn) => new FuncFieldResolver<T>(context => fn(context)!);
}
