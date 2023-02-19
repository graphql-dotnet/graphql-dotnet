#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Instrumentation;

/// <summary>
/// Options for configuring the telemetry instrumentation.
/// </summary>
public class GraphQLTelemetryOptions
{
    // see https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/instrumentation/graphql.md

    /// <summary>
    /// Indicates whether the GraphQL document is included in the telemetry.
    /// </summary>
    public bool RecordDocument { get; set; } = true; // recommended

    /// <summary>
    /// Indicates whether the GraphQL execution is included in the telemetry.
    /// </summary>
    public Func<ExecutionOptions, bool> Filter { get; set; } = _ => true;

    /// <summary>
    /// A delegate which can be used to add additional data to the telemetry at the very beginning of the request
    /// before control falls into actual GraphQL execution engine pipeline.
    /// </summary>
    public Action<Activity, ExecutionOptions> EnrichWithExecutionOptions { get; set; } = (_, _) => { };

    /// <summary>
    /// A delegate which can be used to add additional data to the telemetry after the document has been parsed.
    /// </summary>
    public Action<Activity, ExecutionOptions, ISchema, GraphQLDocument, GraphQLOperationDefinition> EnrichWithDocument { get; set; } = (_, _, _, _, _) => { };

    /// <summary>
    /// A delegate which can be used to add additional data to the telemetry at the conclusion of the request.
    /// </summary>
    public Action<Activity, ExecutionOptions, ExecutionResult> EnrichWithExecutionResult { get; set; } = (_, _, _) => { };

    /// <summary>
    /// A delegate which can be used to add additional data to the telemetry if an unhandled exception occurs
    /// during the execution.
    /// This would almost never occur as GraphQL.NET catches all exceptions and returns them as part of the
    /// execution result, unless <see cref="ExecutionOptions.ThrowOnUnhandledException"/> was set or an
    /// <see cref="OperationCanceledException"/> occurs.
    /// </summary>
    public Action<Activity, Exception> EnrichWithException { get; set; } = (_, _) => { };
}

#endif
