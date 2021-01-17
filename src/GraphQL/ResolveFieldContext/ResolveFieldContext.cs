using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

// THIS FILE CONTAINS OLD CLASSES. THEY ARE USED ONLY FOR TESTS

namespace GraphQL
{
    /// <summary>
    /// A mutable implementation of <see cref="IResolveFieldContext"/>.
    /// </summary>
    public class ResolveFieldContext : IResolveFieldContext //TODO: ??? This class is currently only used for tests
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
        public IDictionary<string, Field> SubFields { get; set; }

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
    public class ResolveFieldContext<TSource> : ResolveFieldContext, IResolveFieldContext<TSource> //TODO: ??? This class is currently only used for tests
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

    public class ResolveEventStreamContext<T> : ResolveFieldContext<T>, IResolveEventStreamContext<T> //TODO: ??? This class is currently only used for tests
    {
        public ResolveEventStreamContext() { }

        public ResolveEventStreamContext(IResolveEventStreamContext context) : base(context) { }
    }

    public class ResolveEventStreamContext : ResolveEventStreamContext<object>, IResolveEventStreamContext //TODO: ??? This class is currently only used for tests
    {
    }
}

namespace GraphQL.Builders
{
    public class ResolveConnectionContext<T> : ResolveFieldContext<T>, IResolveConnectionContext<T> //TODO: ??? This class is currently only used for tests
    {
        private readonly int? _defaultPageSize;

        /// <summary>
        /// Initializes an instance which mirrors the specified <see cref="IResolveFieldContext"/>
        /// with the specified properties and defaults
        /// </summary>
        /// <param name="context">The underlying <see cref="IResolveFieldContext"/> to mirror</param>
        /// <param name="isUnidirectional">Indicates if the connection only allows forward paging requests</param>
        /// <param name="defaultPageSize">Indicates the default page size if not specified by the request</param>
        public ResolveConnectionContext(IResolveFieldContext context, bool isUnidirectional, int? defaultPageSize)
            : base(context)
        {
            IsUnidirectional = isUnidirectional;
            _defaultPageSize = defaultPageSize;
        }

        public bool IsUnidirectional { get; private set; }

        /// <inheritdoc/>
        public int? First
        {
            get
            {
                var first = FirstInternal;
                if (!first.HasValue && !Last.HasValue)
                {
                    return _defaultPageSize;
                }

                return first;
            }
        }

        private int? FirstInternal
        {
            get
            {
                var first = this.GetArgument<int?>("first");
                return first.HasValue ? (int?)Math.Abs(first.Value) : null;
            }
        }

        /// <inheritdoc/>
        public int? Last
        {
            get
            {
                var last = this.GetArgument<int?>("last");
                return last.HasValue ? (int?)Math.Abs(last.Value) : null;
            }
        }

        /// <inheritdoc/>
        public string After => this.GetArgument<string>("after");

        /// <inheritdoc/>
        public string Before => this.GetArgument<string>("before");

        /// <inheritdoc/>
        public int? PageSize => First ?? Last ?? _defaultPageSize;
    }
}
