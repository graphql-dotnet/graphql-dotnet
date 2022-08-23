using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using ExecutionContext = GraphQL.Execution.ExecutionContext;

namespace GraphQL
{
    /// <summary>
    /// A readonly implementation of <see cref="IResolveFieldContext"/>.
    /// </summary>
    public class ReadonlyResolveFieldContext : IResolveFieldContext<object?>
    {
        // WARNING: if you add a new field here, then don't forget to clear it in Reset method!
        private ExecutionNode _executionNode;
        private ExecutionContext _executionContext;
        private IDictionary<string, ArgumentValue>? _arguments;
        private IDictionary<string, DirectiveInfo>? _directives;
        private Dictionary<string, (GraphQLField Field, FieldType FieldType)>? _subFields;
        private IResolveFieldContext? _parent;

        /// <summary>
        /// Initializes an instance with the specified <see cref="ExecutionNode"/> and <see cref="ExecutionContext"/>.
        /// </summary>
        public ReadonlyResolveFieldContext(ExecutionNode node, ExecutionContext context)
        {
            _executionNode = node ?? throw new ArgumentNullException(nameof(node));
            _executionContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        internal ReadonlyResolveFieldContext Reset(ExecutionNode? node, ExecutionContext? context)
        {
            _executionNode = node!;
            _executionContext = context!;
            _arguments = null;
            _directives = null;
            _subFields = null;
            _parent = null;

            return this;
        }

        private IDictionary<string, ArgumentValue>? GetArguments()
            => ExecutionHelper.GetArguments(_executionNode.FieldDefinition!.Arguments, _executionNode.Field!.Arguments, _executionContext.Variables);

        private IDictionary<string, DirectiveInfo>? GetDirectives()
            => ExecutionHelper.GetDirectives(_executionNode.Field, _executionContext.Variables, _executionContext.Schema);

        /// <inheritdoc/>
        public object? Source => _executionNode.Source;

        /// <inheritdoc/>
        public GraphQLField FieldAst => _executionNode.Field!;

        /// <inheritdoc/>
        public FieldType FieldDefinition => _executionNode.FieldDefinition!;

        /// <inheritdoc/>
        public IObjectGraphType ParentType => _executionNode.GetParentType(_executionContext.Schema)!;

        /// <inheritdoc/>
        public IResolveFieldContext? Parent
        {
            get
            {
                if (_parent == null)
                {
                    var parent = _executionNode.Parent;
                    while (parent is ArrayExecutionNode)
                        parent = parent.Parent;

                    if (parent != null && parent is not RootExecutionNode)
                        _parent = new ReadonlyResolveFieldContext(parent, _executionContext);
                }

                return _parent;
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, ArgumentValue>? Arguments => _arguments ??= GetArguments();

        /// <inheritdoc/>
        public IDictionary<string, DirectiveInfo>? Directives => _directives ??= GetDirectives();

        /// <inheritdoc/>
        public object? RootValue => _executionContext.RootValue;

        /// <inheritdoc/>
        public ISchema Schema => _executionContext.Schema;

        /// <inheritdoc/>
        public GraphQLDocument Document => _executionContext.Document;

        /// <inheritdoc/>
        public GraphQLOperationDefinition Operation => _executionContext.Operation;

        /// <inheritdoc/>
        public Variables Variables => _executionContext.Variables;

        /// <inheritdoc/>
        public System.Threading.CancellationToken CancellationToken => _executionContext.CancellationToken;

        /// <inheritdoc/>
        public Metrics Metrics => _executionContext.Metrics;

        /// <inheritdoc/>
        public ExecutionErrors Errors => _executionContext.Errors;

        /// <inheritdoc/>
        public IEnumerable<object> Path => _executionNode.Path;

        /// <inheritdoc/>
        public IEnumerable<object> ResponsePath => _executionNode.ResponsePath;

        /// <inheritdoc/>
        public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields => _subFields ??= _executionContext.ExecutionStrategy.GetSubFields(_executionContext, _executionNode);

        /// <inheritdoc/>
        public IDictionary<string, object?> UserContext => _executionContext.UserContext;

        object? IResolveFieldContext.Source => _executionNode.Source;

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object?> InputExtensions => _executionContext.InputExtensions;

        /// <inheritdoc/>
        public IDictionary<string, object?> OutputExtensions => _executionContext.OutputExtensions;

        /// <inheritdoc/>
        public IServiceProvider? RequestServices => _executionContext.RequestServices;

        /// <inheritdoc/>
        public IExecutionArrayPool ArrayPool => _executionContext;

        /// <inheritdoc/>
        public ClaimsPrincipal? User => _executionContext.User;
    }
}
