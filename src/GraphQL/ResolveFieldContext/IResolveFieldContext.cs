using System.Security.Claims;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL;

/// <summary>
/// Contains parameters pertaining to the currently executing <see cref="IFieldResolver"/>.
/// This object is only valid during the execution of the field; it is re-used once the field
/// has resolved. Use <see cref="ResolveFieldContextExtensions.Copy(IResolveFieldContext)"/>
/// if you need to preserve a copy of the context for later use or copy required properties from the context.
/// </summary>
public interface IResolveFieldContext : IProvideUserContext
{
    /// <summary>The <see cref="GraphQLField"/> AST as derived from the query request.</summary>
    public GraphQLField FieldAst { get; }

    /// <summary>The <see cref="FieldType"/> definition specified in the parent graph type.</summary>
    public FieldType FieldDefinition { get; }

    /// <summary>The field's parent graph type.</summary>
    public IObjectGraphType ParentType { get; }

    /// <summary>
    /// Provides access to the parent context (up to the root). This may be needed to get the parameters of parent nodes.
    /// Returns <see langword="null"/> when called on the root.
    /// </summary>
    public IResolveFieldContext? Parent { get; }

    /// <summary>
    /// A dictionary of arguments passed to the field, or <see langword="null"/> if no arguments were defined for the field.
    /// It is recommended to use the <see cref="ResolveFieldContextExtensions.GetArgument{TType}(IResolveFieldContext, string, TType)">GetArgument</see>
    /// and <see cref="ResolveFieldContextExtensions.HasArgument(IResolveFieldContext, string)">HasArgument</see> extension
    /// methods rather than this dictionary, so the names can be converted by the selected <see cref="INameConverter"/>.
    /// </summary>
    public IDictionary<string, ArgumentValue>? Arguments { get; }

    /// <summary>
    /// A dictionary of directives with their arguments passed to the field, or <see langword="null"/> if no directives were defined for the field.
    /// It is recommended to use the <see cref="ResolveFieldContextExtensions.GetDirective(IResolveFieldContext, string)">GetDirective</see>
    /// and <see cref="ResolveFieldContextExtensions.HasDirective(IResolveFieldContext, string)">HasDirective</see> extension
    /// methods rather than this dictionary directly.
    /// </summary>
    public IDictionary<string, DirectiveInfo>? Directives { get; }

    /// <summary>The root value of the graph, as defined by <see cref="ExecutionOptions.Root"/>.</summary>
    public object? RootValue { get; }

    /// <summary>The value of the parent object in the graph.</summary>
    public object? Source { get; }

    /// <summary>The graph schema.</summary>
    public ISchema Schema { get; }

    /// <summary>The current GraphQL request, parsed into an AST document.</summary>
    public GraphQLDocument Document { get; }

    /// <summary>The operation type (i.e. query, mutation, or subscription) of the current GraphQL request.</summary>
    public GraphQLOperationDefinition Operation { get; }

    /// <summary>The input variables of the current GraphQL request.</summary>
    public Variables Variables { get; }

    /// <summary>A <see cref="System.Threading.CancellationToken">CancellationToken</see> to indicate if and when the request has been canceled.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Allows logging of performance metrics.</summary>
    public Metrics Metrics { get; }

    /// <summary>Can be used to return specific errors back to the GraphQL request caller.</summary>
    public ExecutionErrors Errors { get; }

    /// <summary>The path to the current executing field from the request root as it would appear in the query.</summary>
    public IEnumerable<object> Path { get; }

    /// <summary>The path to the current executing field from the request root as it would appear in the response.</summary>
    public IEnumerable<object> ResponsePath { get; }

    /// <summary>
    /// Returns a set of child fields requested for the current field. Note that this set will be completely defined
    /// (when called from field resolver) only for fields of a concrete type (i.e. not interface or union field). For
    /// interface field this method returns requested fields in terms of this interface. For union field this method
    /// returns empty set since we don't know the concrete union member until we get a concrete runtime value from
    /// the resolver.
    /// </summary>
    public Dictionary<string, (GraphQLField Field, FieldType FieldType)>? SubFields { get; }

    /// <summary>
    /// A dictionary of extra information supplied with the GraphQL request.
    /// This is reserved for implementors to extend the protocol however they see fit,
    /// and hence there are no additional restrictions on its contents. Also you may use
    /// <see cref="ResolveFieldContextExtensions.GetInputExtension(IResolveFieldContext, string)">GetInputExtension</see> method.
    /// </summary>
    public IReadOnlyDictionary<string, object?> InputExtensions { get; }

    /// <summary>
    /// The response map may also contain an entry with key extensions. This entry is reserved for implementors to extend the
    /// protocol however they see fit, and hence there are no additional restrictions on its contents. This dictionary is shared
    /// by all running resolvers and is not thread safe. Also you may use <see cref="ResolveFieldContextExtensions.GetOutputExtension(IResolveFieldContext, string)">GetOutputExtension</see>
    /// and <see cref="ResolveFieldContextExtensions.SetOutputExtension(IResolveFieldContext, string, object)">SetOutputExtension</see>
    /// methods.
    /// </summary>
    public IDictionary<string, object?> OutputExtensions { get; }

    /// <summary>The service provider for the executing request.</summary>
    public IServiceProvider? RequestServices { get; }

    /// <summary>
    /// Returns a resource pool from which arrays can be rented during the current execution.
    /// Can be used to return lists of data from field resolvers.
    /// </summary>
    public IExecutionArrayPool ArrayPool { get; }

    /// <inheritdoc cref="IExecutionContext.User"/>
    public ClaimsPrincipal? User { get; }

    /// <summary>
    /// Returns the execution context for the current request.
    /// </summary>
    public IExecutionContext ExecutionContext { get; }
}

/// <inheritdoc cref="IResolveFieldContext"/>
public interface IResolveFieldContext<out TSource> : IResolveFieldContext
{
    /// <inheritdoc cref="IResolveFieldContext.Source"/>
    public new TSource Source { get; }
}
