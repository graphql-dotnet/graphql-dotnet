using System;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI
{


    public class ResolverBuilder<TSourceType, TReturnType>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1> WithType<T1>()
            => new ResolverBuilder<TSourceType, TReturnType, T1>(_builder, _scoped);

        public ResolverBuilder<TSourceType, TReturnType> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
            => _scoped ? _builder.ResolveScoped(resolver) : _builder.Resolve(resolver);

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver)
            => _scoped ? _builder.ResolveScopedAsync(resolver) : _builder.ResolveAsync(resolver);
    }

    public class ResolverBuilder<TSourceType, TReturnType, T1>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithType<T2>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2>(_builder, _scoped);

        public ResolverBuilder<TSourceType, TReturnType, T1> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, TReturnType> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, TReturnType> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, Task<TReturnType>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    public class ResolverBuilder<TSourceType, TReturnType, T1, T2>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithType<T3>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>(_builder, _scoped);

        public ResolverBuilder<TSourceType, TReturnType, T1, T2> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, TReturnType> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, TReturnType> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, Task<TReturnType>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithType<T4>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>(_builder, _scoped);

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, TReturnType> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, TReturnType> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, Task<TReturnType>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithType<T5>()
            => new ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>(_builder, _scoped);

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, TReturnType> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, TReturnType> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, Task<TReturnType>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>());

            return _scoped ? _builder.ResolveScopedAsync(resolver2) : _builder.ResolveAsync(resolver2);
        }
    }

    public class ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _builder;
        private bool _scoped;

        public ResolverBuilder(FieldBuilder<TSourceType, TReturnType> builder, bool scoped)
        {
            _builder = builder;
            _scoped = scoped;
        }

        public ResolverBuilder<TSourceType, TReturnType, T1, T2, T3, T4, T5> WithScope()
        {
            _scoped = true;
            return this;
        }

        public FieldBuilder<TSourceType, TReturnType> Resolve(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, TReturnType> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, TReturnType> resolver2 =
                (context) => resolver(
                    context,
                    context.RequestServices.GetRequiredService<T1>(),
                    context.RequestServices.GetRequiredService<T2>(),
                    context.RequestServices.GetRequiredService<T3>(),
                    context.RequestServices.GetRequiredService<T4>(),
                    context.RequestServices.GetRequiredService<T5>());

            return _scoped ? _builder.ResolveScoped(resolver2) : _builder.Resolve(resolver2);
        }

        public FieldBuilder<TSourceType, TReturnType> ResolveAsync(Func<IResolveFieldContext<TSourceType>, T1, T2, T3, T4, T5, Task<TReturnType>> resolver)
        {
            Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver2 =
                (context) => resolver(
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
