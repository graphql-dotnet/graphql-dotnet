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
        CancellationToken CancellationToken { get; set; }
        Document Document { get; set; }
        ExecutionErrors Errors { get; set; }
        Fragments Fragments { get; set; }
        List<IDocumentExecutionListener> Listeners { get; set; }
        int? MaxParallelExecutionCount { get; set; }
        Metrics Metrics { get; set; }
        Operation Operation { get; set; }
        object RootValue { get; set; }
        ISchema Schema { get; set; }
        bool ThrowOnUnhandledException { get; set; }
        Action<UnhandledExceptionContext> UnhandledExceptionDelegate { get; set; }
        Variables Variables { get; set; }
    }
}
