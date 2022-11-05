#pragma warning disable IDE0039 // Use local function

using GraphQL.Builders;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// A builder for connection field resolvers with no extra service types.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <summary>
    /// Initializes a new instance with the specified properties.
    /// </summary>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <summary>
    /// Specifies a type that is to be resolved via dependency injection during the resolver's execution.
    /// </summary>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1> WithService<T1>()
        => new(_builder, _scoped);

    /// <summary>
    /// Specifies types that are to be resolved via dependency injection during the resolver's execution.
    /// </summary>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2> WithServices<T1, T2>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithServices<T1, T2, T3>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithServices<T1, T2, T3, T4>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithServices<T1, T2, T3, T4, T5>()
        => new(_builder, _scoped);

    /// <summary>
    /// Specifies that the resolver should run within its own dependency injection scope.
    /// </summary>
    public ConnectionResolverBuilder<TSourceType, TReturnType> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <summary>
    /// Specifies the delegate to execute when the field is being resolved.
    /// </summary>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver)
    {
        if (_scoped)
            _builder.ResolveScoped(resolver);
        else
            _builder.Resolve(resolver);
    }

    /// <inheritdoc cref="Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver)
    {
        if (_scoped)
            _builder.ResolveScopedAsync(resolver);
        else
            _builder.ResolveAsync(resolver);
    }
}

/// <summary>
/// A builder for connection field resolvers with 1 extra service type.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType, T1>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ConnectionResolverBuilder(ConnectionBuilder{TSourceType, TReturnType}, bool)"/>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2> WithService<T2>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, T1, TReturnType?> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>());

        if (_scoped)
            _builder.ResolveScoped(resolver2);
        else
            _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, T1, Task<TReturnType?>> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>());

        if (_scoped)
            _builder.ResolveScopedAsync(resolver2);
        else
            _builder.ResolveAsync(resolver2);
    }
}

/// <summary>
/// A builder for connection field resolvers with 2 extra service types.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ConnectionResolverBuilder(ConnectionBuilder{TSourceType, TReturnType}, bool)"/>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithService<T3>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, T1, T2, TReturnType?> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>());

        if (_scoped)
            _builder.ResolveScoped(resolver2);
        else
            _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, T1, T2, Task<TReturnType?>> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>());

        if (_scoped)
            _builder.ResolveScopedAsync(resolver2);
        else
            _builder.ResolveAsync(resolver2);
    }
}

/// <summary>
/// A builder for connection field resolvers with 3 extra service types.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ConnectionResolverBuilder(ConnectionBuilder{TSourceType, TReturnType}, bool)"/>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithService<T4>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, TReturnType?> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>());

        if (_scoped)
            _builder.ResolveScoped(resolver2);
        else
            _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, Task<TReturnType?>> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>());

        if (_scoped)
            _builder.ResolveScopedAsync(resolver2);
        else
            _builder.ResolveAsync(resolver2);
    }
}

/// <summary>
/// A builder for connection field resolvers with 4 extra service types.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ConnectionResolverBuilder(ConnectionBuilder{TSourceType, TReturnType}, bool)"/>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithService<T5>()
        => new(_builder, _scoped);

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, T4, TReturnType?> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>(),
                context.RequestServices.GetRequiredService<T4>());

        if (_scoped)
            _builder.ResolveScoped(resolver2);
        else
            _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, T4, Task<TReturnType?>> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>(),
                context.RequestServices.GetRequiredService<T4>());

        if (_scoped)
            _builder.ResolveScopedAsync(resolver2);
        else
            _builder.ResolveAsync(resolver2);
    }
}

/// <summary>
/// A builder for connection field resolvers with 5 extra service types.
/// </summary>
public class ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>
{
    private readonly ConnectionBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ConnectionResolverBuilder(ConnectionBuilder{TSourceType, TReturnType}, bool)"/>
    public ConnectionResolverBuilder(ConnectionBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ConnectionResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveConnectionContext{TSourceType}, TReturnType})"/>
    public void Resolve(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, T4, T5, TReturnType?> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>(),
                context.RequestServices.GetRequiredService<T4>(),
                context.RequestServices.GetRequiredService<T5>());

        if (_scoped)
            _builder.ResolveScoped(resolver2);
        else
            _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ConnectionResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveConnectionContext{TSourceType}, Task{TReturnType}})"/>
    public void ResolveAsync(Func<IResolveConnectionContext<TSourceType>, T1, T2, T3, T4, T5, Task<TReturnType?>> resolver)
    {
        Func<IResolveConnectionContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices.GetRequiredService<T1>(),
                context.RequestServices.GetRequiredService<T2>(),
                context.RequestServices.GetRequiredService<T3>(),
                context.RequestServices.GetRequiredService<T4>(),
                context.RequestServices.GetRequiredService<T5>());

        if (_scoped)
            _builder.ResolveScopedAsync(resolver2);
        else
            _builder.ResolveAsync(resolver2);
    }
}
