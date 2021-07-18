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
    /// This object is only valid during the execution of the field; it is re-used once the field
    /// has resolved. Use <see cref="ResolveFieldContextExtensions.Copy(IResolveFieldContext)"/>
    /// if you need to preserve a copy of the context for later use or copy required properties from the context.
    /// </summary>
    public interface IResolveFieldContext : IProvideUserContext
    {
        /// <summary>The <see cref="Field"/> AST as derived from the query request.</summary>
        Field FieldAst { get; }

        /// <summary>The <see cref="FieldType"/> definition specified in the parent graph type.</summary>
        FieldType FieldDefinition { get; }

        /// <summary>The field's parent graph type.</summary>
        IObjectGraphType ParentType { get; }

        /// <summary>
        /// Provides access to the parent context (up to the root). This may be needed to get the parameters of parent nodes.
        /// Returns <see langword="null"/> when called on the root.
        /// </summary>
        IResolveFieldContext? Parent { get; }

        /// <summary>
        /// A dictionary of arguments passed to the field, or <see langword="null"/> if no arguments were defined for the field.
        /// It is recommended to use the <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
        /// and <see cref="ResolveFieldContextExtensions.HasArgument(IResolveFieldContext, string)">HasArgument</see> extension
        /// methods rather than this dictionary, so the names can be converted by the selected <see cref="INameConverter"/>.
        /// </summary>
        IDictionary<string, ArgumentValue>? Arguments { get; }

        /// <summary>The root value of the graph, as defined by <see cref="ExecutionOptions.Root"/>.</summary>
        object? RootValue { get; }

        /// <summary>The value of the parent object in the graph.</summary>
        object? Source { get; }

        /// <summary>The graph schema.</summary>
        ISchema Schema { get; }

        /// <summary>The current GraphQL request, parsed into an AST document.</summary>
        Document Document { get; }

        /// <summary>The operation type (i.e. query, mutation, or subscription) of the current GraphQL request.</summary>
        Operation Operation { get; }

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
        Dictionary<string, Field>? SubFields { get; }

        /// <summary>
        /// The response map may also contain an entry with key extensions. This entry is reserved for implementors to extend the
        /// protocol however they see fit, and hence there are no additional restrictions on its contents. This dictionary is shared
        /// by all running resolvers and is not thread safe. Also you may use <see cref="ResolveFieldContextExtensions.GetExtension(IResolveFieldContext, string)">GetExtension</see>
        /// and <see cref="ResolveFieldContextExtensions.SetExtension(IResolveFieldContext, string, object)">SetExtension</see>
        /// methods.
        /// </summary>
        IDictionary<string, object?> Extensions { get; }

        /// <summary>The service provider for the executing request.</summary>
        IServiceProvider? RequestServices { get; }

        /// <summary>
        /// Returns a resource pool from which arrays can be rented during the current execution.
        /// Can be used to return lists of data from field resolvers.
        /// </summary>
        IExecutionArrayPool ArrayPool { get; }
    }

    /// <inheritdoc cref="IResolveFieldContext"/>
    public interface IResolveFieldContext<out TSource> : IResolveFieldContext
    {
        /// <inheritdoc cref="IResolveFieldContext.Source"/>
        new TSource? Source { get; }
    }
}
