using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Resolvers
{
    /// <summary>
    /// A precompiled source stream resolver for a specific <see cref="MethodInfo"/>.
    /// Calls the specified method (with the specified arguments) and returns the value of the method.
    /// </summary>
    public class SourceStreamMethodResolver : MemberResolver, ISourceStreamResolver
    {
        private Func<IResolveFieldContext, ValueTask<IObservable<object?>>> _sourceStreamResolver = null!;

        /// <summary>
        /// Initializes an instance for the specified method, using the specified instance expression to access the instance of the method,
        /// along with a list of arguments to be passed to the method. The method argument expressions must have return types that match
        /// those of the method arguments.
        /// <br/><br/>
        /// An example of an instance expression would be as follows:
        /// <code>context =&gt; (TSourceType)context.Source</code>
        /// </summary>
        public SourceStreamMethodResolver(MethodInfo methodInfo, LambdaExpression instanceExpression, IList<LambdaExpression> methodArgumentExpressions)
            : base(methodInfo, instanceExpression, methodArgumentExpressions)
        {
        }

        /// <inheritdoc/>
        protected override Func<IResolveFieldContext, ValueTask<object?>> BuildFieldResolver(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            _sourceStreamResolver = BuildSourceStreamResolver(resolveFieldContextParameter, bodyExpression);
            return context => new ValueTask<object?>(context.Source);
        }

        /// <summary>
        /// Creates an appropriate event stream resolver function based on the return type of the expression body.
        /// </summary>
        protected virtual Func<IResolveFieldContext, ValueTask<IObservable<object?>>> BuildSourceStreamResolver(ParameterExpression resolveFieldContextParameter, Expression bodyExpression)
        {
            Expression? taskBodyExpression = null;

            if (bodyExpression.Type == typeof(ValueTask<IObservable<object?>>))
            {
                taskBodyExpression = bodyExpression;
            }
            else if (bodyExpression.Type == typeof(Task<IObservable<object?>>))
            {
                var valueTaskType = typeof(ValueTask<IObservable<object?>>);
                var constructor = valueTaskType.GetConstructor(new Type[] { typeof(Task<IObservable<object?>>) })!;
                taskBodyExpression = Expression.New(
                    constructor,
                    bodyExpression);
            }
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var type = bodyExpression.Type.GetGenericArguments()[0];
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    if (!innerType.IsValueType)
                    {
                        taskBodyExpression = Expression.Call(_castFromTaskAsyncMethodInfo.MakeGenericMethod(innerType), bodyExpression);
                    }
                }
            }
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var type = bodyExpression.Type.GetGenericArguments()[0];
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    if (!innerType.IsValueType)
                    {
                        taskBodyExpression = Expression.Call(_castFromValueTaskAsyncMethodInfo.MakeGenericMethod(innerType), bodyExpression);
                    }
                }
            }
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                var innerType = bodyExpression.Type.GetGenericArguments()[0];
                if (!innerType.IsValueType)
                {
                    var valueTaskType = typeof(ValueTask<IObservable<object?>>);
                    var constructor = valueTaskType.GetConstructor(new Type[] { typeof(IObservable<object?>) })!;
                    taskBodyExpression = Expression.New(
                        constructor,
                        bodyExpression);
                }
            }

            if (taskBodyExpression == null)
            {
                throw new InvalidOperationException("Method must return a IObservable<T> or Task<IObservable<T>> where T is a reference type.");
            }

            var lambda = Expression.Lambda<Func<IResolveFieldContext, ValueTask<IObservable<object?>>>>(taskBodyExpression, resolveFieldContextParameter);
            return lambda.Compile();
        }

        private static readonly MethodInfo _castFromValueTaskAsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromValueTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromValueTaskAsync<T>(ValueTask<IObservable<T>> task) where T : class
            => await task.ConfigureAwait(false);

        private static readonly MethodInfo _castFromTaskAsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromTaskAsync<T>(Task<IObservable<T>> task) where T : class
            => await task.ConfigureAwait(false);

        /// <inheritdoc cref="ISourceStreamResolver.ResolveAsync(IResolveFieldContext)" />
        public ValueTask<IObservable<object?>> ResolveStreamAsync(IResolveFieldContext context) => _sourceStreamResolver(context);

        ValueTask<IObservable<object?>> ISourceStreamResolver.ResolveAsync(IResolveFieldContext context) => ResolveStreamAsync(context);
    }
}
