using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Extensions.DI.Microsoft
{
    internal class ScopedResolveFieldContextAdapter<TSource> : ScopedResolveFieldContextAdapter, IResolveFieldContext<TSource>
    {
        private readonly IResolveFieldContext<TSource> _baseContext;

        public ScopedResolveFieldContextAdapter(IResolveFieldContext<TSource> context, IServiceProvider serviceProvider) : base(context, serviceProvider)
        {
            _baseContext = context;
        }

        TSource IResolveFieldContext<TSource>.Source => _baseContext.Source;
    }

    internal class ScopedResolveFieldContextAdapter : IResolveFieldContext
    {
        private readonly IResolveFieldContext _baseContext;

        public ScopedResolveFieldContextAdapter(IResolveFieldContext context, IServiceProvider serviceProvider)
        {
            _baseContext = context;
            RequestServices = serviceProvider;
        }

        public object Source => _baseContext.Source;

        public string FieldName => _baseContext.FieldName;

        public Language.AST.Field FieldAst => _baseContext.FieldAst;

        public FieldType FieldDefinition => _baseContext.FieldDefinition;

        public IGraphType ReturnType => _baseContext.ReturnType;

        public IObjectGraphType ParentType => _baseContext.ParentType;

        public IDictionary<string, object> Arguments => _baseContext.Arguments;

        public object RootValue => _baseContext.RootValue;

        public ISchema Schema => _baseContext.Schema;

        public Document Document => _baseContext.Document;

        public Operation Operation => _baseContext.Operation;

        public Fragments Fragments => _baseContext.Fragments;

        public Variables Variables => _baseContext.Variables;

        public CancellationToken CancellationToken => _baseContext.CancellationToken;

        public Metrics Metrics => _baseContext.Metrics;

        public ExecutionErrors Errors => _baseContext.Errors;

        public IEnumerable<object> Path => _baseContext.Path;

        public IDictionary<string, Language.AST.Field> SubFields => _baseContext.SubFields;

        public IServiceProvider RequestServices { get; }

        public IDictionary<string, object> UserContext => _baseContext.UserContext;

        public IDictionary<string, object> Extensions => _baseContext.Extensions;

        object IResolveFieldContext.Source => _baseContext.Source;
    }
}
