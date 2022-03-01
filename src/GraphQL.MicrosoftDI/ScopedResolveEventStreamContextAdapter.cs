using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Subscription;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.MicrosoftDI
{
    internal sealed class ScopedResolveEventStreamContextAdapter : IResolveEventStreamContext
    {
        private readonly IResolveEventStreamContext _baseContext;

        public ScopedResolveEventStreamContextAdapter(IResolveEventStreamContext baseContext, IServiceProvider serviceProvider)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
            RequestServices = serviceProvider;
        }

        public object? Source => _baseContext.Source;

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

        public IServiceProvider RequestServices { get; }

        public IDictionary<string, object?> UserContext => _baseContext.UserContext;

        public IReadOnlyDictionary<string, object?> InputExtensions => _baseContext.InputExtensions;

        public IDictionary<string, object?> OutputExtensions => _baseContext.OutputExtensions;

        public IExecutionArrayPool ArrayPool => _baseContext.ArrayPool;
    }
}
