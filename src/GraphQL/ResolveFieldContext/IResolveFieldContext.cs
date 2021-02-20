using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Resolvers;
using GraphQL.Types;
using Field = GraphQL.Language.AST.Field;

namespace GraphQL
{
    /// <summary>
    /// Contains parameters pertaining to the currently executing <see cref="IFieldResolver"/>.
    /// </summary>
    public interface IResolveFieldContext : IProvideUserContext
    {
        /// <summary>The name of the field being resolved.</summary>
        [Obsolete("Will be removed in v4. Use IResolveFieldContext.FieldAst.Name instead.")]
        string FieldName { get; }

        /// <summary>The <see cref="Field"/> AST as derived from the query request.</summary>
        Field FieldAst { get; }

        /// <summary>The <see cref="FieldType"/> definition specified in the parent graph type.</summary>
        FieldType FieldDefinition { get; }

        /// <summary>The return value's graph type.</summary>
        [Obsolete("Will be removed in v4. Use IResolveFieldContext.FieldDefinition.ResolvedType instead.")]
        IGraphType ReturnType { get; }

        /// <summary>The field's parent graph type.</summary>
        IObjectGraphType ParentType { get; }

        /// <summary>
        /// A dictionary of arguments passed to the field. It is recommended to use the
        /// <see cref="GraphQL.ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
        /// and <see cref="GraphQL.ResolveFieldContextExtensions.HasArgument(IResolveFieldContext, string)">HasArgument</see> extension
        /// methods rather than this dictionary, so the names can be converted by the selected <see cref="INameConverter"/>.
        /// </summary>
        IDictionary<string, object> Arguments { get; }

        /// <summary>The root value of the graph, as defined by <see cref="ExecutionOptions.Root"/>.</summary>
        object RootValue { get; }

        /// <summary>The value of the parent object in the graph.</summary>
        object Source { get; }

        /// <summary>The graph schema.</summary>
        ISchema Schema { get; }

        /// <summary>The current GraphQL request, parsed into an AST document.</summary>
        Document Document { get; }

        /// <summary>The operation type (i.e. query, mutation, or subscription) of the current GraphQL request.</summary>
        Operation Operation { get; }

        /// <summary>Returns the query fragments associated with the current GraphQL request.</summary>
        Fragments Fragments { get; }

        /// <summary>The input variables of the current GraphQL request.</summary>
        Variables Variables { get; }

        /// <summary>A <see cref="System.Threading.CancellationToken">CancellationToken</see> to indicate if and when the request has been canceled.</summary>
        CancellationToken CancellationToken { get; }

        /// <summary>Allows logging of performance metrics.</summary>
        Metrics Metrics { get; }

        /// <summary>Can be used to return specific errors back to the GraphQL request caller.</summary>
        ExecutionErrors Errors { get; }

        /// <summary>The path to the current executing field from the request root as it would appear in the query.</summary>
        IEnumerable<object> Path { get; }

        /// <summary>The path to the current executing field from the request root as it would appear in the response.</summary>
        IEnumerable<object> ResponsePath { get; }

        /// <summary>Returns a list of child fields requested for the current field.</summary>
        IDictionary<string, Field> SubFields { get; }

        /// <summary>
        /// The response map may also contain an entry with key extensions. This entry is reserved for implementors to extend the
        /// protocol however they see fit, and hence there are no additional restrictions on its contents. This dictionary is shared
        /// by all running resolvers and is not thread safe. Also you may use <see cref="ResolveFieldContextExtensions.GetExtension(IResolveFieldContext, string)">GetExtension</see>
        /// and <see cref="ResolveFieldContextExtensions.SetExtension(IResolveFieldContext, string, object)">SetExtension</see>
        /// methods.
        /// </summary>
        IDictionary<string, object> Extensions { get; }

        /// <summary>The service provider for the executing request.</summary>
        IServiceProvider RequestServices { get; }
    }

    /// <inheritdoc cref="IResolveFieldContext"/>
    public interface IResolveFieldContext<out TSource> : IResolveFieldContext
    {
        /// <inheritdoc cref="IResolveFieldContext.Source"/>
        new TSource Source { get; }
    }
}
