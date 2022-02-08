using GraphQL.Builders;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{
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
            => new ResolverBuilder<TSourceType, TReturnType, T1>(_builder, _scoped);

        /// <summary>
        /// Specifies types that are to be resolved via dependency injection during the resolver's execution.
        /// </summary>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithServices<T1, T2>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2>(_builder, _scoped);

        /// <inheritdoc cref="WithServices{T1, T2}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithServices<T1, T2, T3>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>(_builder, _scoped);

        /// <inheritdoc cref="WithServices{T1, T2}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithServices<T1, T2, T3, T4>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>(_builder, _scoped);

        /// <inheritdoc cref="WithServices{T1, T2}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithServices<T1, T2, T3, T4, T5>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>(_builder, _scoped);

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
    }

    /// <summary>
    /// A builder for field resolvers with 1 extra service type.
    /// </summary>
    public class ResolverBuilder<TSourceType, TReturnType, T1>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolverBuilder(FieldBuilder{TSourceType, TReturnType}, bool)"/>
        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithService<T2>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2>(_builder, _scoped);

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
                    context.RequestServices.GetRequiredService<T1>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, Task<TReturnType?>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
                context => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    /// <summary>
    /// A builder for field resolvers with 2 extra service types.
    /// </summary>
    public class ResolverBuilder<TSourceType, TReturnType, T1, T2>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolverBuilder(FieldBuilder{TSourceType, TReturnType}, bool)"/>
        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithService<T3>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>(_builder, _scoped);

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
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, Task<TReturnType?>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
                context => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    /// <summary>
    /// A builder for field resolvers with 3 extra service types.
    /// </summary>
    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolverBuilder(FieldBuilder{TSourceType, TReturnType}, bool)"/>
        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithService<T4>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>(_builder, _scoped);

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
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, Task<TReturnType?>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
                context => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    /// <summary>
    /// A builder for field resolvers with 4 extra service types.
    /// </summary>
    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolverBuilder(FieldBuilder{TSourceType, TReturnType}, bool)"/>
        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.WithService{T1}"/>
        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithService<T5>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>(_builder, _scoped);

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
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, Task<TReturnType?>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
                context => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    /// <summary>
    /// A builder for field resolvers with 5 extra service types.
    /// </summary>
    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolverBuilder(FieldBuilder{TSourceType, TReturnType}, bool)"/>
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
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>(),
                    context.RequestServices.GetRequiredService<T5>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        /// <inheritdoc cref="ResolverBuilder{TSourceType, TReturnType}.ResolveAsync(Func{IResolveFieldContext{TSourceType}, Task{TReturnType}})"/>
        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, Task<TReturnType?>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType?>> resolver2 =
                context => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>(),
                    context.RequestServices.GetRequiredService<T5>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }
}
