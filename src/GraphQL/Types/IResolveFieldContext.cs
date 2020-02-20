using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using System.Collections.Generic;
using System.Threading;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL.Types
{
    /// <summary>
    /// Contains parameters pertaining to the currently executing <see cref="IFieldResolver"/>
    /// </summary>
    public interface IResolveFieldContext : IProvideUserContext
    {
        /// <summary>The name of the field being resolved</summary>
        string FieldName { get; }

        /// <summary>The <see cref="Field"/> AST as derived from the query request</summary>
        Field FieldAst { get; }

        /// <summary>The <see cref="FieldType"/> definition specified in the parent graph type</summary>
        FieldType FieldDefinition { get; }

        /// <summary>The return value's graph type</summary>
        IGraphType ReturnType { get; }

        /// <summary>The field's parent graph type</summary>
        IObjectGraphType ParentType { get; }

        /// <summary>
        /// A dictionary of arguments passed to the field. It is recommended to use the
        /// <see cref="GraphQL.ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
        /// and <see cref="GraphQL.ResolveFieldContextExtensions.HasArgument(IResolveFieldContext, string)">HasArgument</see> extension
        /// functions rather than this dictionary, so the names can be converted by the selected <see cref="IFieldNameConverter"/>.
        /// </summary>
        IDictionary<string, object> Arguments { get; }

        /// <summary>The root value of the graph, as defined by <see cref="ExecutionContext.RootValue"/></summary>
        object RootValue { get; }

        /// <summary>The value of the parent object in the graph</summary>
        object Source { get; }

        /// <summary>The graph schema</summary>
        ISchema Schema { get; }

        /// <summary>The current GraphQL request, parsed into an AST document</summary>
        Document Document { get; }

        /// <summary>The operation type (i.e. query, mutation, or subscription) of the current GraphQL request</summary>
        Operation Operation { get; }

        Fragments Fragments { get; }

        /// <summary>The input variables of the current GraphQL request</summary>
        Variables Variables { get; }

        /// <inheritdoc cref="GraphQL.Execution.ExecutionOptions.CancellationToken"/>
        CancellationToken CancellationToken { get; }

        Metrics Metrics { get; }

        ExecutionErrors Errors { get; }

        IEnumerable<string> Path { get; }

        IDictionary<string, Field> SubFields { get; }
    }

    /// <inheritdoc cref="IResolveFieldContext"/>
    public interface IResolveFieldContext<out TSource> : IResolveFieldContext
    {
        /// <inheritdoc cref="IResolveFieldContext.Source"/>
        new TSource Source { get; }
    }
}
