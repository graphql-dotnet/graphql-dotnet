using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using System.Collections.Generic;
using System.Threading;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Types
{
    public interface IResolveFieldContext : IProvideUserContext
    {
        public string FieldName { get; }

        public Field FieldAst { get; }

        public FieldType FieldDefinition { get; }

        public IGraphType ReturnType { get; }

        public IObjectGraphType ParentType { get; }

        public Dictionary<string, object> Arguments { get; }

        public object RootValue { get; }

        public object Source { get; }

        public ISchema Schema { get; }

        public Document Document { get; }

        public Operation Operation { get; }

        public Fragments Fragments { get; }

        public Variables Variables { get; }

        public CancellationToken CancellationToken { get; }

        public Metrics Metrics { get; }

        public ExecutionErrors Errors { get; }

        public IEnumerable<string> Path { get; }

        public IDictionary<string, Field> SubFields { get; }

    }

    public interface IResolveFieldContext<out TSource> : IResolveFieldContext
    {
        new public TSource Source { get; }
    }
}
