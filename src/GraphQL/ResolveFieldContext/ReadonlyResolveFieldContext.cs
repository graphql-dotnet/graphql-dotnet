#nullable enable

using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL
{
    /// <summary>
    /// A readonly implementation of <see cref="IResolveFieldContext"/>.
    /// </summary>
    public class ReadonlyResolveFieldContext : IResolveFieldContext<object>
    {
        // WARNING: if you add a new field here, then don't forget to clear it in Reset method!
        private ExecutionNode _executionNode;
        private ExecutionContext _executionContext;
        private IDictionary<string, ArgumentValue>? _arguments;
        private Dictionary<string, Field>? _subFields;
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
            _subFields = null;
            _parent = null;

            return this;
        }

        private IDictionary<string, ArgumentValue>? GetArguments()
            => ExecutionHelper.GetArgumentValues(_executionNode.FieldDefinition!.Arguments, _executionNode.Field!.Arguments, _executionContext.Variables);

        /// <inheritdoc/>
        public object? Source => _executionNode.Source;

        /// <inheritdoc/>
        public Field FieldAst => _executionNode.Field!;

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

                    if (parent != null && !(parent is RootExecutionNode))
                        _parent = new ReadonlyResolveFieldContext(parent, _executionContext);
                }

                return _parent;
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, ArgumentValue>? Arguments => _arguments ??= GetArguments();

        /// <inheritdoc/>
        public object? RootValue => _executionContext.RootValue;

        /// <inheritdoc/>
        public ISchema Schema => _executionContext.Schema;

        /// <inheritdoc/>
        public Document Document => _executionContext.Document;

        /// <inheritdoc/>
        public Operation Operation => _executionContext.Operation;

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
        public Dictionary<string, Field>? SubFields => _subFields ??= _executionContext.ExecutionStrategy.GetSubFields(_executionContext, _executionNode);

        /// <inheritdoc/>
        public IDictionary<string, object?> UserContext => _executionContext.UserContext;

        object? IResolveFieldContext.Source => _executionNode.Source;

        /// <inheritdoc/>
        public IDictionary<string, object?> Extensions => _executionContext.Extensions;

        /// <inheritdoc/>
        public IServiceProvider? RequestServices => _executionContext.RequestServices;

        /// <inheritdoc/>
        public IExecutionArrayPool ArrayPool => _executionContext;
    }
}
