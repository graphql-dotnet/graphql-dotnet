using System;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// When resolving a field, this implementation calls a predefined <see cref="Func{T, TResult}"/> and returns the result
    /// </summary>
    public class FuncFieldResolver<TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext, TReturnType> _resolver;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a field.
        /// </summary>
        public FuncFieldResolver(Func<IResolveFieldContext, TReturnType> resolver)
        {
            _resolver = resolver;
        }

        /// <inheritdoc/>
        public TReturnType Resolve(IResolveFieldContext context) => _resolver(context);

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }

    /// <summary>
    /// <inheritdoc cref="FuncFieldResolver{TReturnType}"/>
    /// <br/><br/>
    /// This implementation provides a typed <see cref="IResolveFieldContext{TSource}"/> to the resolver function.
    /// </summary>
    public class FuncFieldResolver<TSourceType, TReturnType> : IFieldResolver<TReturnType>
    {
        private readonly Func<IResolveFieldContext, TReturnType> _resolver;
        private static ResolveFieldContextAdapter<TSourceType> _sharedAdapter;

        /// <inheritdoc cref="FuncFieldResolver{TReturnType}.FuncFieldResolver(Func{IResolveFieldContext, TReturnType})"/>
        public FuncFieldResolver(Func<IResolveFieldContext<TSourceType>, TReturnType> resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver), "A resolver function must be specified");

            if (typeof(TSourceType) == typeof(object) ||                               // ReadonlyResolveFieldContext implements IResolveFieldContext<object> so we can cast the context
                typeof(TReturnType) == typeof(object) ||                               // The return type cannot be determined to not be a data loader or task
                typeof(IDataLoaderResult).IsAssignableFrom(typeof(TReturnType)) ||     // Data loaders cannot use pooled contexts
                typeof(Task).IsAssignableFrom(typeof(TReturnType)))                    // Asynchronous tasks cannot use pooled contexts
            {
                _resolver = (context) => resolver(context.As<TSourceType>());
            }
            else
            {
                _resolver = (context) =>
                {
                    var adapter = System.Threading.Interlocked.Exchange(ref _sharedAdapter, null);
                    adapter = adapter == null ? new ResolveFieldContextAdapter<TSourceType>(context) : adapter.Set(context);
                    var ret = resolver(adapter);
                    adapter.Reset();
                    System.Threading.Interlocked.CompareExchange(ref _sharedAdapter, adapter, null);
                    return ret;
                };
            }
        }

        /// <inheritdoc/>
        public TReturnType Resolve(IResolveFieldContext context) => _resolver(context);

        object IFieldResolver.Resolve(IResolveFieldContext context) => Resolve(context);
    }
}
