using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
    /// <summary>
    /// Provides information regarding the currently executing document.
    /// </summary>
    public interface IExecutionContext : IProvideUserContext
    {
        /// <summary>
        /// Propagates notification that the GraphQL request should be canceled
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// The parsed GraphQL request
        /// </summary>
        Document Document { get; }

        /// <summary>
        /// A list of errors generated during GraphQL request processing
        /// </summary>
        ExecutionErrors Errors { get; }

        /// <summary>
        /// A list of <see cref="FragmentDefinition"/>s that pertain to the GraphQL request
        /// </summary>
        Fragments Fragments { get; }

        /// <summary>
        /// A list of <see cref="IDocumentExecutionListener"/>s, enabling code to be executed at various points during the processing of the GraphQL query
        /// </summary>
        List<IDocumentExecutionListener> Listeners { get; }

        /// <summary>
        /// If set, limits the maximum number of nodes (in other words GraphQL fields) executed in parallel
        /// </summary>
        int? MaxParallelExecutionCount { get; }

        /// <summary>
        /// Provides performance metrics logging capabilities
        /// </summary>
        Metrics Metrics { get; }

        /// <summary>
        /// The GraphQL operation that is being executed
        /// </summary>
        Operation Operation { get; }

        /// <summary>
        /// Object to pass to the <see cref="IResolveFieldContext.Source"/> property of first-level resolvers
        /// </summary>
        object RootValue { get; }

        /// <summary>
        /// Schema of the graph to use
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// When <c>false</c>, <see cref="DocumentExecuter"/> and <see cref="ExecutionStrategy"/> capture unhandled
        /// exceptions and store them within <see cref="Errors">Errors</see>
        /// </summary>
        bool ThrowOnUnhandledException { get; }

        /// <summary>
        /// A delegate that can override, hide, modify, or log unhandled exceptions before they are stored
        /// within <see cref="Errors"/> as an <see cref="ExecutionError"/>.
        /// </summary>
        Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; }

        /// <summary>
        /// Input variables to the GraphQL request
        /// </summary>
        Variables Variables { get; }

        /// <summary>
        /// The response map may also contain an entry with key extensions. This entry is reserved for implementors to extend the
        /// protocol however they see fit, and hence there are no additional restrictions on its contents.
        /// </summary>
        Dictionary<string, object> Extensions { get; }

        /// <summary>
        /// The service provider for the executing request. Typically this is a scoped service provider
        /// from your dependency injection framework.
        /// </summary>
        IServiceProvider RequestServices { get; }
    }
}
