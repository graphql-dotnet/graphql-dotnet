using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// A builder for field resolvers with no extra service types.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType>
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <summary>
    /// Initializes a new instance with the specified properties.
    /// </summary>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <summary>
    /// Specifies a type that is to be resolved via dependency injection during the resolver's execution.
    /// </summary>
    public ResolverBuilder<TSourceType, TReturnType, T1> WithService<T1>()
        where T1 : notnull
        => new(_builder, _scoped);

    /// <summary>
    /// Specifies types that are to be resolved via dependency injection during the resolver's execution.
    /// </summary>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithServices<T1, T2>()
        where T1 : notnull
        where T2 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithServices<T1, T2, T3>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithServices<T1, T2, T3, T4>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="WithServices{T1, T2}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithServices<T1, T2, T3, T4, T5>()
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where T4 : notnull
        where T5 : notnull
        => new(_builder, _scoped);

    /// <summary>
    /// Specifies that the resolver should run within its own dependency injection scope.
    /// </summary>
    public ResolverBuilder<TSourceType, TReturnType> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <summary>
    /// Specifies the delegate to execute when the field is being resolved.
    /// </summary>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        => _scoped ? _builder.ResolveScoped(resolver) : _builder.Resolve(resolver);

    /// <inheritdoc cref="Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver)
        => _scoped ? _builder.ResolveScopedAsync(resolver) : _builder.ResolveAsync(resolver);

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}

/// <summary>
/// A builder for field resolvers with 1 extra service type.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType, T1>
    where T1 : notnull
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, bool)"/>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithService<T2>()
        where T2 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ResolverBuilder<TSourceType, TReturnType, T1> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, TReturnType?> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>());

        _builder.FieldType.DependsOn(typeof(T1));
        return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, Task<TReturnType?>> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>());

        _builder.FieldType.DependsOn(typeof(T1));
        return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
    }

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>, T1> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}

/// <summary>
/// A builder for field resolvers with 2 extra service types.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType, T1, T2>
    where T1 : notnull
    where T2 : notnull
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, bool)"/>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithService<T3>()
        where T3 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, TReturnType?> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, Task<TReturnType?>> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
    }

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>, T1, T2> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}

/// <summary>
/// A builder for field resolvers with 3 extra service types.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, bool)"/>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithService<T4>()
        where T4 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, TReturnType?> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, Task<TReturnType?>> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
    }

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>, T1, T2, T3> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}

/// <summary>
/// A builder for field resolvers with 4 extra service types.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, bool)"/>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithService<T5>()
        where T5 : notnull
        => new(_builder, _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, TReturnType?> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>(),
                context.RequestServices!.GetRequiredService<T4>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        _builder.FieldType.DependsOn(typeof(T4));
        return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, Task<TReturnType?>> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>(),
                context.RequestServices!.GetRequiredService<T4>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        _builder.FieldType.DependsOn(typeof(T4));
        return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
    }

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>, T1, T2, T3, T4> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}

/// <summary>
/// A builder for field resolvers with 5 extra service types.
/// </summary>
public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>
    where T1 : notnull
    where T2 : notnull
    where T3 : notnull
    where T4 : notnull
    where T5 : notnull
{
    private readonly FieldBuilder<TSourceType, TReturnType> _builder;
    private bool _scoped;

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}(FieldBuilder{TSourceType, TReturnType}, bool)"/>
    public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
    {
        _builder = builder;
        _scoped = scoped;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithScope"/>
    public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithScope()
    {
        _scoped = true;
        return this;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.Resolve(Func{IResolveFieldContext{TSourceType}, TReturnType})"/>
    public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, TReturnType?> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>(),
                context.RequestServices!.GetRequiredService<T4>(),
                context.RequestServices!.GetRequiredService<T5>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        _builder.FieldType.DependsOn(typeof(T4));
        _builder.FieldType.DependsOn(typeof(T5));
        return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, Task<TReturnType?>> resolver)
    {
        Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
            context => resolver(
                context,
                context.RequestServices!.GetRequiredService<T1>(),
                context.RequestServices!.GetRequiredService<T2>(),
                context.RequestServices!.GetRequiredService<T3>(),
                context.RequestServices!.GetRequiredService<T4>(),
                context.RequestServices!.GetRequiredService<T5>());

        _builder.FieldType.DependsOn(typeof(T1));
        _builder.FieldType.DependsOn(typeof(T2));
        _builder.FieldType.DependsOn(typeof(T3));
        _builder.FieldType.DependsOn(typeof(T4));
        _builder.FieldType.DependsOn(typeof(T5));
        return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
    }

    private ResolverBuilder<TSourceType, IDataLoaderResult<TReturnType>, T1, T2, T3, T4, T5> ReturnsDataLoader()
        => new(_builder.ReturnsDataLoader(), _scoped);

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, IDataLoaderResult<TReturnType>?> resolver)
    {
        ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, IDataLoaderResult<IDataLoaderResult<TReturnType>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }

    /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
    public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>?> resolver)
    {
        ReturnsDataLoader().ReturnsDataLoader().ReturnsDataLoader().Resolve(resolver);
        return _builder;
    }
}
