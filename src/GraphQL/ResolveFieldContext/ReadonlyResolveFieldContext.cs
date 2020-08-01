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
    /// A readonly implementation of <see cref="IResolveFieldContext{object}"/>
    /// </summary>
    public class ReadonlyResolveFieldContext : IResolveFieldContext<object>
    {
        private readonly ExecutionNode _executionNode;
        private readonly ExecutionContext _executionContext;
        private IDictionary<string, object> _arguments;
        private IDictionary<string, Field> _subFields;

        public ReadonlyResolveFieldContext(ExecutionNode node, ExecutionContext context)
        {
            _executionNode = node ?? throw new ArgumentNullException(nameof(node));
            _executionContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        private IDictionary<string, Field> GetSubFields()
            => ExecutionHelper.SubFieldsFor(_executionContext, _executionNode.FieldDefinition.ResolvedType, _executionNode.Field);

        private IDictionary<string, object> GetArguments()
            => ExecutionHelper.GetArgumentValues(_executionContext.Schema, _executionNode.FieldDefinition.Arguments, _executionNode.Field.Arguments, _executionContext.Variables);

        public object Source => _executionNode.Source;

        public string FieldName => _executionNode.Field.Name;

        public Field FieldAst => _executionNode.Field;

        public FieldType FieldDefinition => _executionNode.FieldDefinition;

        public IGraphType ReturnType => _executionNode.FieldDefinition.ResolvedType;

        public IObjectGraphType ParentType => _executionNode.GetParentType(_executionContext.Schema);

        public IDictionary<string, object> Arguments => _arguments ?? (_arguments = GetArguments());

        public object RootValue => _executionContext.RootValue;

        public ISchema Schema => _executionContext.Schema;

        public Document Document => _executionContext.Document;

        public Operation Operation => _executionContext.Operation;

        public Fragments Fragments => _executionContext.Fragments;

        public Variables Variables => _executionContext.Variables;

        public System.Threading.CancellationToken CancellationToken => _executionContext.CancellationToken;

        public Metrics Metrics => _executionContext.Metrics;

        public ExecutionErrors Errors => _executionContext.Errors;

        public IEnumerable<object> Path => _executionNode.Path;

        public IEnumerable<object> ResponsePath => _executionNode.ResponsePath;

        public IDictionary<string, Field> SubFields => _subFields ?? (_subFields = GetSubFields());

        public IDictionary<string, object> UserContext => _executionContext.UserContext;

        object IResolveFieldContext.Source => _executionNode.Source;

        public IDictionary<string, object> Extensions => _executionContext.Extensions;
    }
}
