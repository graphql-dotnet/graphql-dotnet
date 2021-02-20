using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.MicrosoftDI
{
    internal sealed class ScopedResolveFieldContextAdapter<TSource> : IResolveFieldContext<TSource>
    {
        private static readonly bool _acceptNulls = !typeof(TSource).IsValueType || (typeof(TSource).IsGenericType && typeof(TSource).GetGenericTypeDefinition() == typeof(Nullable<>));

        private readonly IResolveFieldContext _baseContext;

        public ScopedResolveFieldContextAdapter(IResolveFieldContext baseContext, IServiceProvider serviceProvider)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
            if (baseContext.Source == null && !_acceptNulls)
            {
                throw new ArgumentException("baseContext.Source is null and cannot be cast to non-nullable value type " + typeof(TSource).Name, nameof(baseContext));
            }
            else
            {
                try
                {
                    Source = (TSource)baseContext.Source;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("baseContext.Source is not of type " + typeof(TSource).Name, nameof(baseContext));
                }
            }
            RequestServices = serviceProvider;
        }

        public TSource Source { get; }

        public Field FieldAst => _baseContext.FieldAst;

        public FieldType FieldDefinition => _baseContext.FieldDefinition;

        public IObjectGraphType ParentType => _baseContext.ParentType;

        public IDictionary<string, ArgumentValue> Arguments => _baseContext.Arguments;

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

        public Fields SubFields => _baseContext.SubFields;

        public IServiceProvider RequestServices { get; }

        public IDictionary<string, object> UserContext => _baseContext.UserContext;

        public IDictionary<string, object> Extensions => _baseContext.Extensions;

        object IResolveFieldContext.Source => _baseContext.Source;

        public IExecutionArrayPool ArrayPool => _baseContext.ArrayPool;
    }
}
