using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL
{
    internal sealed class ResolveFieldContextAdapter<T> : IResolveFieldContext<T>
    {
        private IResolveFieldContext _baseContext;
        private static readonly bool _acceptNulls = !typeof(T).IsValueType || typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// Creates an instance that maps to the specified base <see cref="IResolveFieldContext"/>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the <see cref="IResolveFieldContext.Source"/> property cannot be cast to the specified type</exception>
        public ResolveFieldContextAdapter(IResolveFieldContext baseContext)
        {
            Set(baseContext);
        }

        internal void Reset()
        {
            _baseContext = null;
            Source = default;
        }

        internal ResolveFieldContextAdapter<T> Set(IResolveFieldContext baseContext)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));

            if (baseContext.Source == null && !_acceptNulls)
            {
                throw new ArgumentException("baseContext.Source is null and cannot be cast to non-nullable value type " + typeof(T).Name, nameof(baseContext));
            }
            else
            {
                try
                {
                    Source = (T)baseContext.Source;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("baseContext.Source is not of type " + typeof(T).Name, nameof(baseContext));
                }
            }

            return this;
        }

        public T Source { get; private set; }

        public Field FieldAst => _baseContext.FieldAst;

        public FieldType FieldDefinition => _baseContext.FieldDefinition;

        public IObjectGraphType ParentType => _baseContext.ParentType;

        public IResolveFieldContext Parent => _baseContext.Parent;

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

        public IDictionary<string, object> UserContext => _baseContext.UserContext;

        public IDictionary<string, object> Extensions => _baseContext.Extensions;

        object IResolveFieldContext.Source => Source;

        public IServiceProvider RequestServices => _baseContext.RequestServices;

        /// <inheritdoc/>
        public IExecutionArrayPool ArrayPool => _baseContext.ArrayPool;
    }
}
