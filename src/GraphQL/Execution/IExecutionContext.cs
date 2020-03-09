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
        CancellationToken CancellationToken { get; }

        Document Document { get; }

        ExecutionErrors Errors { get; }

        Fragments Fragments { get; }

        List<IDocumentExecutionListener> Listeners { get; }

        int? MaxParallelExecutionCount { get; }

        Metrics Metrics { get; }

        Operation Operation { get; }

        object RootValue { get; }

        ISchema Schema { get; }

        bool ThrowOnUnhandledException { get; }

        Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; }

        Variables Variables { get; }
    }
}
