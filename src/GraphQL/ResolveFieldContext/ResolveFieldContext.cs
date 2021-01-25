using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL
{
    /// <summary>
    /// A mutable implementation of <see cref="IResolveFieldContext"/>
    /// </summary>
    public class ResolveFieldContext : IResolveFieldContext<object>
    {
        /// <inheritdoc/>
        public string FieldName { get; set; }

        /// <inheritdoc/>
        public Field FieldAst { get; set; }

        /// <inheritdoc/>
        public FieldType FieldDefinition { get; set; }

        /// <inheritdoc/>
        public IGraphType ReturnType { get; set; }

        /// <inheritdoc/>
        public IObjectGraphType ParentType { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, ArgumentValue> Arguments { get; set; }

        /// <inheritdoc/>
        public object RootValue { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object> UserContext { get; set; }

        /// <inheritdoc/>
        public object Source { get; set; }

        /// <inheritdoc/>
        public ISchema Schema { get; set; }

        /// <inheritdoc/>
        public Document Document { get; set; }

        /// <inheritdoc/>
        public Operation Operation { get; set; }

        /// <inheritdoc/>
        public Fragments Fragments { get; set; }

        /// <inheritdoc/>
        public Variables Variables { get; set; }

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get; set; }

        /// <inheritdoc/>
        public Metrics Metrics { get; set; }

        /// <inheritdoc/>
        public ExecutionErrors Errors { get; set; }

        /// <inheritdoc/>
        public IEnumerable<object> Path { get; set; }

        /// <inheritdoc/>
        public IEnumerable<object> ResponsePath { get; set; }

        /// <inheritdoc/>
        public Fields SubFields { get; set; }

        /// <inheritdoc/>
        public IServiceProvider RequestServices { get; set; }

        /// <inheritdoc/>
        public IDictionary<string, object> Extensions { get; set; }

        /// <inheritdoc/>
        public IExecutionArrayPool ArrayPool { get; set; }

        /// <summary>
        /// Initializes a new instance with all fields set to their default values.
        /// </summary>
        public ResolveFieldContext() { }

        /// <summary>
        /// Clone the specified <see cref="IResolveFieldContext"/>.
        /// </summary>
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
            ResponsePath = context.ResponsePath;
            RequestServices = context.RequestServices;
            Extensions = context.Extensions;
            ArrayPool = context.ArrayPool;
        }
    }

    /// <inheritdoc cref="ResolveFieldContext"/>
    public class ResolveFieldContext<TSource> : ResolveFieldContext, IResolveFieldContext<TSource>
    {
        /// <inheritdoc cref="ResolveFieldContext()"/>
        public ResolveFieldContext()
        {
        }

        /// <summary>
        /// Clone the specified <see cref="IResolveFieldContext"/>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the <see cref="IResolveFieldContext.Source"/> property cannot be cast to <typeparamref name="TSource"/></exception>
        public ResolveFieldContext(IResolveFieldContext context) : base(context)
        {
            if (context.Source != null && !(context.Source is TSource))
                throw new ArgumentException($"IResolveFieldContext.Source must be an instance of type '{typeof(TSource).Name}'", nameof(context));
        }

        /// <inheritdoc cref="ResolveFieldContext.Source"/>
        public new TSource Source
        {
            get => (TSource)base.Source;
            set => base.Source = value;
        }
    }
}
