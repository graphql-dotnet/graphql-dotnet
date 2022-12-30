#if NET5_0_OR_GREATER

namespace GraphQL.Instrumentation;

/// <summary>
/// Options for configuring the OpenTelemetry tracing instrumentation.
/// </summary>
public class TelemetryOptions
{
    /// <summary>
    /// Indicates whether the GraphQL document is included in the OpenTelemetry activity.
    /// </summary>
    public bool RecordDocument { get; set; }
}

#endif
