using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using System;
using System.Collections.Generic;
using System.Threading;
using Field = GraphQL.Language.AST.Field;
using GraphQL.Execution;

namespace GraphQL.Types
{
    public class ResolveFieldContext : IResolveFieldContext, IProvideUserContext
    {
        public string FieldName { get; set; }

        public Field FieldAst { get; set; }

        public FieldType FieldDefinition { get; set; }

        public IGraphType ReturnType { get; set; }

        public IObjectGraphType ParentType { get; set; }

        public IDictionary<string, object> Arguments { get; set; }

        public object RootValue { get; set; }

        public IDictionary<string, object> UserContext { get; set; }

        public object Source { get; set; }

        public ISchema Schema { get; set; }

        public Document Document { get; set; }

        public Operation Operation { get; set; }

        public Fragments Fragments { get; set; }

        public Variables Variables { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public Metrics Metrics { get; set; }

        public ExecutionErrors Errors { get; set; }

        public IEnumerable<string> Path { get; set; }

        /// <summary>
        /// Queried sub fields
        /// </summary>
        public IDictionary<string, Field> SubFields { get; set; }

        public ResolveFieldContext() { }

        public ResolveFieldContext(IResolveFieldContext context)
        {
            Source = context.Source;
            FieldName = context.FieldName;
            FieldAst = context.FieldAst;
            FieldDefinition = context.FieldDefinition;
            ReturnType = context.ReturnType;
            ParentType = context.ParentType;
            Arguments = context.Arguments;
            Schema = context.Schema;
            Document = context.Document;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Metrics = context.Metrics;
            Errors = context.Errors;
            SubFields = context.SubFields;
            Path = context.Path;
        }
    }

    public class ResolveFieldContext<TSource> : ResolveFieldContext, IResolveFieldContext<TSource>
    {
        public ResolveFieldContext()
        {
        }

        public ResolveFieldContext(IResolveFieldContext context) : base(context)
        {
            if (context.Source != null && !(context.Source is TSource))
                throw new ArgumentException($"IResolveFieldContext.Source must be an instance of type '{typeof(TSource).Name}'", nameof(context));
        }

        public new TSource Source
        {
            get => (TSource)base.Source;
            set => base.Source = value;
        }

        public ResolveFieldContext(GraphQL.Execution.ExecutionContext context, Field field, FieldType type, TSource source, IObjectGraphType parentType, Dictionary<string, object> arguments, IEnumerable<string> path)
        {
            Source = source;
            FieldName = field.Name;
            FieldAst = field;
            FieldDefinition = type;
            ReturnType = type.ResolvedType;
            ParentType = parentType;
            Arguments = arguments;
            Schema = context.Schema;
            Document = context.Document;
            Fragments = context.Fragments;
            RootValue = context.RootValue;
            UserContext = context.UserContext;
            Operation = context.Operation;
            Variables = context.Variables;
            CancellationToken = context.CancellationToken;
            Metrics = context.Metrics;
            Errors = context.Errors;
            Path = path;
        }
    }
}
