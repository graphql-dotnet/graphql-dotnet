using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Subscription;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A precompiled event stream resolver for a specific <see cref="MethodInfo"/>.
    /// Calls the specified method (with the specified arguments) and returns the value of the method.
    /// </summary>
    internal class EventStreamMethodResolver : MemberResolver, IAsyncEventStreamResolver
    {
        private Func<IResolveFieldContext, Task<IObservable<object?>>> _eventStreamResolver = null!;

        /// <summary>
        /// Initializes an instance for the specified method and arguments, using a default instance expression of:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// The method argument expressions must have return types that match those of the method arguments.
        /// </summary>
        public EventStreamMethodResolver(MethodInfo methodInfo, IList<LambdaExpression> methodArgumentExpressions)
            : this(methodInfo, methodArgumentExpressions, null)
        {
        }

        /// <summary>
        /// Initializes an instance for the specified method, using the specified instance expression to access the instance of the method,
        /// along with a list of arguments to be passed to the method. The method argument expressions must have return types that match
        /// those of the method arguments.
        /// If <paramref name="instanceExpression"/> is <see langword="null"/> then a default instance expression is used as follows:
        /// <code>context =&gt; (SourceType)context.Source</code>
        /// </summary>
        public EventStreamMethodResolver(MethodInfo methodInfo, IList<LambdaExpression> methodArgumentExpressions, LambdaExpression? instanceExpression)
            : base(methodInfo, methodArgumentExpressions, instanceExpression)
        {
        }

        /// <inheritdoc/>
        protected override Func<IResolveFieldContext, object?> BuildFieldResolver(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            if (bodyExpression.Type == typeof(Task<IObservable<object?>>))
            {
                return Complete(bodyExpression);
            }
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var type = bodyExpression.Type.GetGenericArguments()[0];
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    if (!innerType.IsValueType)
                    {
                        return Complete(Expression.Call(_castFromTaskAsyncMethodInfo.MakeGenericMethod(innerType), bodyExpression));
                    }
                }
            }
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                var innerType = bodyExpression.Type.GetGenericArguments()[0];
                if (!innerType.IsValueType)
                {
                    return Complete(Expression.Call(_castFromObservableMethodInfo.MakeGenericMethod(innerType), bodyExpression));
                }
            }

            throw new InvalidOperationException("Method must return a IObservable<T> or Task<IObservable<T>> where T is a reference type.");

            Func<IResolveFieldContext, object?> Complete(Expression bodyExpression)
            {
                var lambda = Expression.Lambda<Func<IResolveFieldContext, Task<IObservable<object?>>>>(bodyExpression, resolveFieldContextParameter);
                _eventStreamResolver = lambda.Compile();
                return context => context.Source;
            }
        }

        private static readonly MethodInfo _castFromTaskAsyncMethodInfo = typeof(EventStreamMethodResolver).GetMethod(nameof(CastFromTaskAsync), BindingFlags.Static | BindingFlags.NonPublic);
        private static async Task<IObservable<object?>> CastFromTaskAsync<T>(Task<IObservable<T>> task) where T : class
            => await task.ConfigureAwait(false);

        private static readonly MethodInfo _castFromObservableMethodInfo = typeof(EventStreamMethodResolver).GetMethod(nameof(CastFromObservable), BindingFlags.Static | BindingFlags.NonPublic);
        private static Task<IObservable<object?>> CastFromObservable<T>(IObservable<T> observable) where T : class
            => Task.FromResult<IObservable<object?>>(observable);

        /// <inheritdoc/>
        public Task<IObservable<object?>> SubscribeAsync(IResolveEventStreamContext context) => _eventStreamResolver(context);
    }
}
