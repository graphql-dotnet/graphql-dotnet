using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.DI
{
    internal class ContextWrapper<T> : IResolveFieldContext<T>
    {
        private readonly IResolveFieldContext<T> _baseContext;

        public ContextWrapper(IResolveFieldContext<T> baseContext, IServiceProvider serviceProvider)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
            RequestServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public T Source => _baseContext.Source;

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

        public IEnumerable<object> ResponsePath => _baseContext.ResponsePath;

        public IDictionary<string, Language.AST.Field> SubFields => _baseContext.SubFields;

        public IDictionary<string, object> Extensions => _baseContext.Extensions;

        public IServiceProvider RequestServices { get; }

        public IDictionary<string, object> UserContext => _baseContext.UserContext;

        object IResolveFieldContext.Source => ((IResolveFieldContext)_baseContext).Source;
    }

    internal class ContextWrapper : IResolveFieldContext
    {
        private readonly IResolveFieldContext _baseContext;

        public ContextWrapper(IResolveFieldContext baseContext, IServiceProvider serviceProvider)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
            RequestServices = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        } 

        public string FieldName => _baseContext.FieldName;

        public Language.AST.Field FieldAst => _baseContext.FieldAst;

        public FieldType FieldDefinition => _baseContext.FieldDefinition;

        public IGraphType ReturnType => _baseContext.ReturnType;

        public IObjectGraphType ParentType => _baseContext.ParentType;

        public IDictionary<string, object> Arguments => _baseContext.Arguments;

        public object RootValue => _baseContext.RootValue;

        public object Source => _baseContext.Source;

        public ISchema Schema => _baseContext.Schema;

        public Document Document => _baseContext.Document;

        public Operation Operation => _baseContext.Operation;

        public Fragments Fragments => _baseContext.Fragments;

        public Variables Variables => _baseContext.Variables;

        public CancellationToken CancellationToken => _baseContext.CancellationToken;

        public Metrics Metrics => _baseContext.Metrics;

        public ExecutionErrors Errors => _baseContext.Errors;

        public IEnumerable<object> Path => _baseContext.Path;

        public IEnumerable<object> ResponsePath => _baseContext.ResponsePath;

        public IDictionary<string, Language.AST.Field> SubFields => _baseContext.SubFields;

        public IDictionary<string, object> Extensions => _baseContext.Extensions;

        public IServiceProvider RequestServices { get; }

        public IDictionary<string, object> UserContext => _baseContext.UserContext;
    }
}
