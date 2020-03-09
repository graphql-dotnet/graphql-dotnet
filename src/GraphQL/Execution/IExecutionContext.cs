using System;
using System.Collections.Generic;
using System.Threading;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;

namespace GraphQL.Execution
{
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
        /// When false, <see cref="DocumentExecuter"/> and <see cref="ExecutionStrategy"/> captures unhandled
        /// exceptions and stores them within <see cref="Errors">Errors</see>
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
    }
}
