#if NET5_0_OR_GREATER

using GraphQL.Telemetry;

namespace OpenTelemetry.AutoInstrumentation;

internal class Initializer
{
    internal static bool Enabled { get; set; }
    internal static GraphQLTelemetryOptions? Options { get; set; }

    public static void EnableAutoInstrumentation(GraphQLTelemetryOptions options)
    {
        Enabled = true;
        Options = options;
    }
}

#endif
