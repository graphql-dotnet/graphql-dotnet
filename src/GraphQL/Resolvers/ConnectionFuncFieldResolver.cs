using System;
using GraphQL.Execution;

namespace GraphQL.Resolvers
{
    public class ConnectionFuncFieldResolver<TSourceType> : IFieldResolver, IResolveFieldContextProvider
    {
        private readonly Func<IResolveConnectionContext<TSourceType>, object> _resolver;
        private readonly bool _isUnidirectional;
        private readonly int? _defaultPageSize;

        /// <summary>
        /// Initializes a new instance that runs the specified delegate when resolving a field.
        /// </summary>
        public ConnectionFuncFieldResolver(Func<IResolveConnectionContext<TSourceType>, object> resolver, bool isUnidirectional, int? defaultPageSize)
        {
            _resolver = resolver;
            _isUnidirectional = isUnidirectional;
            _defaultPageSize = defaultPageSize;
        }

        public IResolveFieldContext CreateContext(ExecutionNode node, ExecutionContext context)
        {
            var c = new ReadonlyResolveConnectionContext<TSourceType>(node, context, _isUnidirectional, _defaultPageSize);
            CheckForErrors(c);
            return c;
        }

        /// <inheritdoc/>
        public object Resolve(IResolveFieldContext context)
        {
            return context is IResolveConnectionContext<TSourceType> typedContext
                ? _resolver(typedContext)
                : _resolver(new Builders.ResolveConnectionContext<TSourceType>(context, _isUnidirectional, _defaultPageSize)); //TODO: needed only for tests
                //: throw new ArgumentException($"Context must be of '{typeof(IResolveConnectionContext<TSourceType>).Name}' type. Use {typeof(IResolveFieldContextProvider).Name} to create context.", nameof(context));
        }

        private void CheckForErrors(IResolveConnectionContext<TSourceType> context)
        {
            if (context.First.HasValue && context.Last.HasValue)
            {
                throw new ArgumentException("Cannot specify both `first` and `last`.");
            }
            if (context.IsUnidirectional && context.Last.HasValue)
            {
                throw new ArgumentException("Cannot use `last` with unidirectional connections.");
            }
        }
    }
}
