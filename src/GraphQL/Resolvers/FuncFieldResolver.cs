using System.Collections;
using GraphQL.DataLoader;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result
    /// </summary>
    public class FuncFieldResolver<TReturnType> : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a field.
        /// </summary>
        public FuncFieldResolver(Func<IResolveFieldContext, TReturnType?> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var returnType = resolver.Method.ReturnType; // in case the delegate was cast so TReturnType is just object, pull the original return type
            if (typeof(Task).IsAssignableFrom(returnType) || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                throw new InvalidOperationException("Please use the proper FuncFieldResolver constructor for asynchronous delegates, or call FieldAsync when adding your field to the graph.");

            _resolver = context => new ValueTask<object?>(resolver(context));
        }

        /// <inheritdoc cref="FuncFieldResolver{TReturnType}.FuncFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
        public FuncFieldResolver(Func<IResolveFieldContext, ValueTask<TReturnType?>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _resolver = resolver is Func<IResolveFieldContext, ValueTask<object?>> resolverObject
                ? resolverObject
                : async context => await resolver(context).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => _resolver(context);
    }

    /// <summary>
    /// <inheritdoc cref="FuncFieldResolver{TReturnType}"/>
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver
    {
        private readonly Func<IResolveFieldContext, ValueTask<object?>> _resolver;
        private static ResolveFieldContextAdapter<TSourceType>? _sharedAdapter;

        /// <inheritdoc cref="FuncFieldResolver{TReturnType}.FuncFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
        public FuncFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _resolver = GetResolverFor(resolver);
        }

        /// <inheritdoc cref="FuncFieldResolver{TReturnType}.FuncFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
        public FuncFieldResolver(Func<IResolveFieldContext<TSourceType>, ValueTask<TReturnType?>> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _resolver = resolver is Func<IResolveFieldContext<TSourceType>, ValueTask<object?>> resolverObject
                ? context => resolverObject(context.As<TSourceType>())
                : async context => await resolver(context.As<TSourceType>()).ConfigureAwait(false);
        }

        private Func<IResolveFieldContext, ValueTask<object?>> GetResolverFor(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        {
            var returnType = resolver.Method.ReturnType; // in case the delegate was cast so TReturnType is just object, pull the original return type
            if (typeof(Task).IsAssignableFrom(returnType) || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                throw new InvalidOperationException("Please use the proper FuncFieldResolver constructor for asynchronous delegates, or call FieldAsync when adding your field to the graph.");

            // also see ExecutionStrategy.ExecuteNodeAsync as it relates to context re-use

            // when source type is object, just pass the context through, letting the execution strategy handle context re-use
            if (typeof(TSourceType) == typeof(object))
            {
                // ReadonlyResolveFieldContext implements IResolveFieldContext<object>
                return (context) => new ValueTask<object?>(resolver(context.As<TSourceType>()));
            }

            // for return types of IDataLoaderResult or IEnumerable
            if (!CanReuseContextForType(returnType))
            {
                // Data loaders and IEnumerable results cannot use pooled contexts
                return (context) => new ValueTask<object?>(resolver(context.As<TSourceType>()));
            }

            // for return types of object, examine the return type at runtime to determine if context re-use is applicable
            if (returnType == typeof(object))
            {
                // must determine type at runtime
                return (context) =>
                {
                    var adapter = Interlocked.Exchange(ref _sharedAdapter, null);
                    if (adapter == null)
                    {
                        adapter = new ResolveFieldContextAdapter<TSourceType>(context);
                    }
                    else
                    {
                        adapter.Set(context);
                    }
                    var ret = resolver(adapter);
                    // only re-use contexts that do not return an IDataLoaderResult or an IEnumerable (that may be based on the context source)
                    if (CanReuseContextForValue(ret))
                    {
                        adapter.Reset();
                        Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                    }
                    return new ValueTask<object?>(ret);
                };
            }

            // TSourceType is not object
            // TReturnType is not an IEnumerable, IDataLoaderResult, or object
            // use a pooled context
            return (context) =>
            {
                var adapter = Interlocked.Exchange(ref _sharedAdapter, null);
                adapter = adapter == null ? new ResolveFieldContextAdapter<TSourceType>(context) : adapter.Set(context);
                var ret = resolver(adapter);
                adapter.Reset();
                Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                return new ValueTask<object?>(ret);
            };

            static bool CanReuseContextForType(Type type) => !IsDataLoaderType(type) && !IsEnumerableType(type);
            static bool CanReuseContextForValue(object? value) => !IsDataLoaderValue(value) && !IsEnumerableValue(value);

            static bool IsEnumerableType(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
            static bool IsEnumerableValue(object? value) => value is IEnumerable && value is not string;

            static bool IsDataLoaderType(Type type) => typeof(IDataLoaderResult).IsAssignableFrom(type);
            static bool IsDataLoaderValue(object? value) => value is IDataLoaderResult;
        }

        /// <inheritdoc/>
        public ValueTask<object?> ResolveAsync(IResolveFieldContext context) => _resolver(context);
    }
}
