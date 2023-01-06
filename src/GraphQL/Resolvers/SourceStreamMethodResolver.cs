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
            // ValueTask<IObservable<object?>>
            if (bodyExpression.Type == typeof(ValueTask<IObservable<object?>>))
            {
                return Compile(bodyExpression);
            }

            // Task<IObservable<object?>>
            if (bodyExpression.Type == typeof(Task<IObservable<object?>>))
            {
                return Compile(Expression.New(_valueTaskObservableCtor, bodyExpression));
            }

            // Task<T>
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var type = bodyExpression.Type.GetGenericArguments()[0];

                // Task<IObservable<T>>
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    var method = innerType.IsValueType ? _castFromTask2AsyncMethodInfo : _castFromTaskAsyncMethodInfo;
                    return Compile(Expression.Call(method.MakeGenericMethod(innerType), bodyExpression));
                }

                // Task<IAsyncEnumerable<T>>
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    var method = _convertFromTaskAsyncEnumerableMethodInfo.MakeGenericMethod(innerType);
                    var func = method.CreateDelegate<Func<Expression, ParameterExpression, Func<IResolveFieldContext, ValueTask<IObservable<object?>>>>>();
                    return func(bodyExpression, resolveFieldContextParameter);
                }
            }

            // ValueTask<T>
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var type = bodyExpression.Type.GetGenericArguments()[0];

                // ValueTask<IObservable<T>>
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    var method = innerType.IsValueType ? _castFromValueTask2AsyncMethodInfo : _castFromValueTaskAsyncMethodInfo;
                    return Compile(Expression.Call(method.MakeGenericMethod(innerType), bodyExpression));
                }

                // ValueTask<IAsyncEnumerable<T>>
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    var innerType = type.GetGenericArguments()[0];
                    var method = _convertFromValueTaskAsyncEnumerableMethodInfo.MakeGenericMethod(innerType);
                    var func = method.CreateDelegate<Func<Expression, ParameterExpression, Func<IResolveFieldContext, ValueTask<IObservable<object?>>>>>();
                    return func(bodyExpression, resolveFieldContextParameter);
                }

                throw new InvalidOperationException("If method returns ValueTask<T> than T must be of type IObservable<> or IAsyncEnumerable<>");
            }

            // IObservable<T>
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(IObservable<>))
            {
                var innerType = bodyExpression.Type.GetGenericArguments()[0];
                if (innerType.IsValueType)
                {
                    var adapterType = typeof(ObservableAdapter<>).MakeGenericType(innerType);
                    var ctor = adapterType.GetConstructor(new Type[] { bodyExpression.Type })!;
                    bodyExpression = Expression.New(ctor, bodyExpression);
                }
                return Compile(Expression.New(_valueTaskObservableCtor, bodyExpression));
            }

            // IAsyncEnumerable<T>
            else if (bodyExpression.Type.IsGenericType && bodyExpression.Type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                var innerType = bodyExpression.Type.GetGenericArguments()[0];
                var method = _convertFromAsyncEnumerableMethodInfo.MakeGenericMethod(innerType);
                var func = method.CreateDelegate<Func<Expression, ParameterExpression, Func<IResolveFieldContext, ValueTask<IObservable<object?>>>>>();
                return func(bodyExpression, resolveFieldContextParameter);
            }

            throw new InvalidOperationException("Method must return a IObservable<T>, Task<IObservable<T>>, ValueTask<IObservable<T>>, IAsyncEnumerable<T>, Task<IAsyncEnumerable<T>> or ValueTask<IAsyncEnumerable<T>>.");

            Func<IResolveFieldContext, ValueTask<IObservable<object?>>> Compile(Expression taskBodyExpression)
            {
                var lambda = Expression.Lambda<Func<IResolveFieldContext, ValueTask<IObservable<object?>>>>(taskBodyExpression, resolveFieldContextParameter);
                return lambda.Compile();
            }
        }

        private static readonly ConstructorInfo _valueTaskObservableCtor = typeof(ValueTask<IObservable<object?>>).GetConstructor(new Type[] { typeof(Task<IObservable<object?>>) })!;

        private static readonly MethodInfo _convertFromAsyncEnumerableMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(ConvertFromAsyncEnumerable), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> ConvertFromAsyncEnumerable<T>(Expression body, ParameterExpression resolveFieldContextParameter)
        {
            var lambda = Expression.Lambda<Func<IResolveFieldContext, IAsyncEnumerable<T>>>(body, resolveFieldContextParameter);
            var func = lambda.Compile();
            return ObservableFromAsyncEnumerable<T>.Create(func);
        }

        private static readonly MethodInfo _convertFromTaskAsyncEnumerableMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(ConvertFromTaskAsyncEnumerable), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> ConvertFromTaskAsyncEnumerable<T>(Expression body, ParameterExpression resolveFieldContextParameter)
        {
            var lambda = Expression.Lambda<Func<IResolveFieldContext, Task<IAsyncEnumerable<T>>>>(body, resolveFieldContextParameter);
            var func = lambda.Compile();
            return ObservableFromAsyncEnumerable<T>.Create(func);
        }

        private static readonly MethodInfo _convertFromValueTaskAsyncEnumerableMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(ConvertFromValueTaskAsyncEnumerable), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static Func<IResolveFieldContext, ValueTask<IObservable<object?>>> ConvertFromValueTaskAsyncEnumerable<T>(Expression body, ParameterExpression resolveFieldContextParameter)
        {
            var lambda = Expression.Lambda<Func<IResolveFieldContext, ValueTask<IAsyncEnumerable<T>>>>(body, resolveFieldContextParameter);
            var func = lambda.Compile();
            return ObservableFromAsyncEnumerable<T>.Create(func);
        }

        private static readonly MethodInfo _castFromValueTaskAsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromValueTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromValueTaskAsync<T>(ValueTask<IObservable<T>> task) where T : class
            => await task.ConfigureAwait(false);

        private static readonly MethodInfo _castFromValueTask2AsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromValueTask2Async), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromValueTask2Async<T>(ValueTask<IObservable<T>> task)
            => new ObservableAdapter<T>(await task.ConfigureAwait(false));

        private static readonly MethodInfo _castFromTaskAsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromTaskAsync), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromTaskAsync<T>(Task<IObservable<T>> task) where T : class
            => await task.ConfigureAwait(false);

        private static readonly MethodInfo _castFromTask2AsyncMethodInfo = typeof(SourceStreamMethodResolver).GetMethod(nameof(CastFromTask2Async), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static async ValueTask<IObservable<object?>> CastFromTask2Async<T>(Task<IObservable<T>> task)
            => new ObservableAdapter<T>(await task.ConfigureAwait(false));

        /// <inheritdoc cref="ISourceStreamResolver.ResolveAsync(IResolveFieldContext)" />
        public ValueTask<IObservable<object?>> ResolveStreamAsync(IResolveFieldContext context) => _sourceStreamResolver(context);

        ValueTask<IObservable<object?>> ISourceStreamResolver.ResolveAsync(IResolveFieldContext context) => ResolveStreamAsync(context);

        /// <summary>
        /// Converts an <see cref="IObservable{T}"/> for value types into an <see cref="IObservable{T}">IObservable&lt;object?&gt;</see>.
        /// </summary>
        private sealed class ObservableAdapter<T> : IObservable<object?>
        {
            private readonly IObservable<T> _observable;

            public ObservableAdapter(IObservable<T> observable)
            {
                _observable = observable;
            }

            public IDisposable Subscribe(IObserver<object?> observer) => _observable.Subscribe(new ObserverAdapter(observer));

            private sealed class ObserverAdapter : IObserver<T>
            {
                private readonly IObserver<object?> _observer;
                public ObserverAdapter(IObserver<object?> observer)
                {
                    _observer = observer;
                }
                public void OnCompleted() => _observer.OnCompleted();
                public void OnError(Exception error) => _observer.OnError(error);
                public void OnNext(T value) => _observer.OnNext(value); // note: boxing here
            }
        }
    }
}
