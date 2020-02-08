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
        string FieldName { get; }

        Field FieldAst { get; }

        FieldType FieldDefinition { get; }

        IGraphType ReturnType { get; }

        IObjectGraphType ParentType { get; }

        IDictionary<string, object> Arguments { get; }

        object RootValue { get; }

        object Source { get; }

        ISchema Schema { get; }

        Document Document { get; }

        Operation Operation { get; }

        Fragments Fragments { get; }

        Variables Variables { get; }

        CancellationToken CancellationToken { get; }

        Metrics Metrics { get; }

        ExecutionErrors Errors { get; }

        IEnumerable<string> Path { get; }

        IDictionary<string, Field> SubFields { get; }

        object Result { get; set; }
    }

    public interface IResolveFieldContext<out TSource> : IResolveFieldContext
    {
        new TSource Source { get; }
    }
}
