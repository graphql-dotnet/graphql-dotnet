#if NET5_0_OR_GREATER

namespace GraphQL.Instrumentation;

/// <summary>
/// Options for configuring the telemetry instrumentation.
/// </summary>
public class TelemetryOptions
{
    // see https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/instrumentation/graphql.md

    /// <summary>
    /// Indicates whether the GraphQL document is included in the telemetry.
    /// </summary>
    public bool RecordDocument { get; set; } = true; // recommended
}

#endif
