using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    internal class ResolveFieldContextAdapter<T> : IResolveFieldContext<T>
    {
        private readonly IResolveFieldContext _baseContext;

        public ResolveFieldContextAdapter(IResolveFieldContext baseContext)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));

            try
            {
                Source = (T)baseContext.Source;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("baseContext.Source is not of type " + typeof(T).Name, nameof(baseContext));
            }
            catch (NullReferenceException)
            {
                throw new ArgumentException("baseContext.Source is null and cannot be cast to non-nullable value type " + typeof(T).Name, nameof(baseContext));
            }
        }

        public T Source { get; }

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

        public IEnumerable<string> Path => _baseContext.Path;

        public IDictionary<string, Language.AST.Field> SubFields => _baseContext.SubFields;

        public IDictionary<string, object> UserContext => _baseContext.UserContext;

        object IResolveFieldContext.Source => Source;
    }
}
