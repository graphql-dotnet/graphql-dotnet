using System.Collections;
using GraphQL.DataLoader;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result
    /// </summary>
    public class FuncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext, TReturnType?> _resolver;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a field.
        /// </summary>
        public FuncFieldResolver(Func<IResolveFieldContext, TReturnType?> resolver)
        {
            _resolver = resolver;
        }

        /// <inheritdoc/>
        public ValueTask<TReturnType?> ResolveAsync(IResolveFieldContext context)
            => new ValueTask<TReturnType?>(_resolver(context));

        ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => new ValueTask<object?>(_resolver(context));
    }

    /// <summary>
    /// <inheritdoc cref="FuncFieldResolver{TReturnType}"/>
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext, TReturnType?> _resolver;
        private static ResolveFieldContextAdapter<TSourceType>? _sharedAdapter;

        /// <inheritdoc cref="FuncFieldResolver{TReturnType}.FuncFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
        public FuncFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");
            _resolver = GetResolverFor(resolver);
        }

        private Func<IResolveFieldContext, TReturnType?> GetResolverFor(Func<IResolveFieldContext<TSourceType>, TReturnType?> resolver)
        {
            // also see ExecutionStrategy.ExecuteNodeAsync as it relates to context re-use

            // when source type is object, just pass the context through
            if (typeof(TSourceType) == typeof(object))
            {
                // ReadonlyResolveFieldContext implements IResolveFieldContext<object>
                return (context) => resolver(context.As<TSourceType>());
            }

            // for return types of IDataLoaderResult or IEnumerable
            if (!CanReuseContextForType(typeof(TReturnType)))
            {
                // Data loaders and IEnumerable results cannot use pooled contexts
                return (context) => resolver(context.As<TSourceType>());
            }

            // for return types of Task<IDataLoaderResult> or Task<IEnumerable>
            if (typeof(TReturnType).IsGenericType && typeof(TReturnType).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var returnType = typeof(TReturnType).GetGenericArguments()[0];
                if (!CanReuseContextForType(returnType))
                {
                    // Data loaders and IEnumerable results cannot use pooled contexts
                    return (context) => resolver(context.As<TSourceType>());
                }
            }

            // for return types of object or Task<object>
            if (typeof(TReturnType) == typeof(object) || typeof(TReturnType).IsGenericType && typeof(TReturnType).GetGenericTypeDefinition() == typeof(Task<>) && typeof(TReturnType).GetGenericArguments()[0] == typeof(object))
            {
                // must determine type at runtime
                return (context) =>
                {
                    var adapter = System.Threading.Interlocked.Exchange(ref _sharedAdapter, null);
                    if (adapter == null)
                    {
                        adapter = new ResolveFieldContextAdapter<TSourceType>(context);
                    }
                    else
                    {
                        adapter.Set(context);
                    }
                    var ret = resolver(adapter);
                    // only re-use contexts that completed synchronously and do not return an IDataLoaderResult or an IEnumerable (that may be based on the context source)
                    if (ret is Task task)
                    {
                        if (task.IsCompleted && task.Status == TaskStatus.RanToCompletion)
                        {
                            var ret2 = task.GetResult();
                            if (CanReuseContextForValue(ret2))
                            {
                                adapter.Reset();
                                System.Threading.Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                            }
                        }
                    }
                    else if (CanReuseContextForValue(ret))
                    {
                        adapter.Reset();
                        System.Threading.Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                    }
                    return ret;
                };
            }

            // not an IEnumerable, IDataLoaderResult, Task<IEnumerable>, Task<IDataLoaderResult>, object or Task<object>
            // use a pooled context
            if (typeof(Task).IsAssignableFrom(typeof(TReturnType)))
            {
                return (context) =>
                {
                    var adapter = System.Threading.Interlocked.Exchange(ref _sharedAdapter, null);
                    adapter = adapter == null ? new ResolveFieldContextAdapter<TSourceType>(context) : adapter.Set(context);
                    var ret = resolver(adapter);
                    var t = (Task)(object)ret!;
                    if (t.IsCompleted && t.Status == TaskStatus.RanToCompletion)
                    {
                        adapter.Reset();
                        System.Threading.Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                    }
                    return ret;
                };
            }
            else
            {
                return (context) =>
                {
                    var adapter = System.Threading.Interlocked.Exchange(ref _sharedAdapter, null);
                    adapter = adapter == null ? new ResolveFieldContextAdapter<TSourceType>(context) : adapter.Set(context);
                    var ret = resolver(adapter);
                    adapter.Reset();
                    System.Threading.Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                    return ret;
                };
            }

            static bool CanReuseContextForType(Type type) => !IsDataLoaderType(type) && !IsEnumerableType(type);
            static bool CanReuseContextForValue(object? value) => !IsDataLoaderValue(value) && !IsEnumerableValue(value);

            static bool IsEnumerableType(Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
            static bool IsEnumerableValue(object? value) => value is IEnumerable && value is not string;

            static bool IsDataLoaderType(Type type) => typeof(IDataLoaderResult).IsAssignableFrom(type);
            static bool IsDataLoaderValue(object? value) => value is IDataLoaderResult;
        }

        /// <inheritdoc/>
        public ValueTask<TReturnType?> ResolveAsync(IResolveFieldContext context)
            => new ValueTask<TReturnType?>(_resolver(context));

        ValueTask<object?> IFieldResolver.ResolveAsync(IResolveFieldContext context)
            => new ValueTask<object?>(_resolver(context));
    }
}
