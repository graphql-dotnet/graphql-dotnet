using System.Diagnostics.Tracing;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// EventSource events emitted from the project.
/// </summary>
[EventSource(Name = "GraphQL")]
internal class GraphQLEventSource : EventSource
{
    public static readonly GraphQLEventSource Log = new();

    [Event(1, Level = EventLevel.Informational)]
    public void SchemaCreated(Schema schema)
    {
        if (IsEnabled())
        {
            WriteEvent(1, schema.GetType().Name);
        }
    }

    [Event(2, Level = EventLevel.Informational)]
    public void SchemaInitialized(Schema schema)
    {
        if (IsEnabled())
        {
            WriteEvent(2, schema.GetType().Name);
        }
    }

    [Event(2, Message = "Error accured while processing trace event, MySqlTraceEventType: {0}, Message {1}, Exception: {2}", Level = EventLevel.Error)]
    public void ErrorTraceEvent1111111111(string message, string exception)
    {
        if (IsEnabled())
        {
            WriteEvent(1, message, exception);
        }
    }
}
