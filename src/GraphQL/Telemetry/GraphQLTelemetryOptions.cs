#if NET5_0_OR_GREATER

using System.Diagnostics;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Telemetry;

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
    /// Sanitizes recorded GraphQL document to exclude sensitive information when <see cref="RecordDocument"/> is <see langword="true"/>.
    /// </summary>
    public Func<ExecutionOptions, string?> SanitizeDocument { get; set; } = options => options.Query;

    /// <summary>
    /// Indicates whether to collect telemetry about the GraphQL query execution.
    /// If filter returns <see langword="true" />, the telemetry is collected.
    /// If filter returns <see langword="false" />, the telemetry for the GraphQL query
    /// and all the downstream calls is not collected.
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
    /// <para>
    /// This would almost never occur as GraphQL.NET catches all exceptions and returns them as part of the
    /// execution result, unless <see cref="ExecutionOptions.ThrowOnUnhandledException"/> was set or an
    /// <see cref="OperationCanceledException"/> occurs.
    /// </para>
    /// <para>
    /// When using the OpenTelemetry SDK you may use the Activity.RecordException extension method to add additional data.
    /// </para>
    /// </summary>
    public Action<Activity, Exception> EnrichWithException { get; set; } = (_, _) => { };

    // note: when adding properties, be sure corresponding changes are made in GraphQLBuilderBase.cs
}

#endif
