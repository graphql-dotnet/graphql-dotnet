using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL
{
    internal sealed class ResolveFieldContextAdapter<T> : IResolveFieldContext<T>
    {
        private IResolveFieldContext _baseContext;
        private static readonly bool _acceptNulls = !typeof(T).IsValueType || typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// Creates an instance that maps to the specified base <see cref="IResolveFieldContext"/>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the <see cref="IResolveFieldContext.Source"/> property cannot be cast to the specified type</exception>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ResolveFieldContextAdapter(IResolveFieldContext baseContext)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Set(baseContext);
        }

        internal void Reset()
        {
            _baseContext = null!;
            Source = default!;
        }

        internal ResolveFieldContextAdapter<T> Set(IResolveFieldContext baseContext)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));

            if (baseContext.Source == null && !_acceptNulls)
            {
                throw new ArgumentException("baseContext.Source is null and cannot be cast to non-nullable value type " + typeof(T).Name, nameof(baseContext));
            }
            else
            {
                try
                {
                    Source = (T)baseContext.Source!;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("baseContext.Source is not of type " + typeof(T).Name, nameof(baseContext));
                }
            }

            return this;
        }

        public T Source { get; private set; }

        public GraphQLField FieldAst => _baseContext.FieldAst;

        public FieldType FieldDefinition => _baseContext.FieldDefinition;

        public IObjectGraphType ParentType => _baseContext.ParentType;

        public IResolveFieldContext? Parent => _baseContext.Parent;

        public IDictionary<string, ArgumentValue>? Arguments => _baseContext.Arguments;

        public IDictionary<string, DirectiveInfo>? Directives => _baseContext.Directives;

        public object? RootValue => _baseContext.RootValue;

        public ISchema Schema => _baseContext.Schema;

        public GraphQLDocument Document => _baseContext.Document;

        public GraphQLOperationDefinition Operation => _baseContext.Operation;

        public Variables Variables => _baseContext.Variables;

        public CancellationToken CancellationToken => _baseContext.CancellationToken;

        public Metrics Metrics => _baseContext.Metrics;

        public ExecutionErrors Errors => _baseContext.Errors;

        public IEnumerable<object> Path => _baseContext.Path;

        public IEnumerable<object> ResponsePath => _baseContext.ResponsePath;

        public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields => _baseContext.SubFields;

        public IDictionary<string, object?> UserContext => _baseContext.UserContext;

        public IReadOnlyDictionary<string, object?> InputExtensions => _baseContext.InputExtensions;

        public IDictionary<string, object?> OutputExtensions => _baseContext.OutputExtensions;

        object? IResolveFieldContext.Source => Source;

        public IServiceProvider? RequestServices => _baseContext.RequestServices;

        /// <inheritdoc/>
        public IExecutionArrayPool ArrayPool => _baseContext.ArrayPool;
    }
}
