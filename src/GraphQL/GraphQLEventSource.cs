using System.Diagnostics.Tracing;
using GraphQL.Types;

namespace GraphQL;

/// <summary>
/// EventSource events emitted from the project.
/// </summary>
[EventSource(Name = "GraphQL-NET")]
internal sealed class GraphQLEventSource : EventSource
{
    public static GraphQLEventSource Log { get; } = new();

#if DEBUG
    public GraphQLEventSource()
        : base(EventSourceSettings.ThrowOnEventWriteErrors)
    {
    }
#endif

    // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-instrumentation#setting-event-keywords
#pragma warning disable IDE1006 // Naming Styles
    public class Keywords
    {
        public const EventKeywords Schema = (EventKeywords)1;
        public const EventKeywords Parsing = (EventKeywords)2;
        public const EventKeywords Validation = (EventKeywords)4;
        public const EventKeywords Execution = (EventKeywords)8;
        public const EventKeywords Introspection = (EventKeywords)16;
        public const EventKeywords DataLoader = (EventKeywords)32;
    }
#pragma warning restore IDE1006 // Naming Styles

    [NonEvent]
    public void SchemaCreated(Schema schema)
    {
        if (IsEnabled())
        {
            WriteEvent(1, schema.GetType().Name);
        }
    }

    [Event(1, Level = EventLevel.Informational)]
    public void SchemaCreated(string schemaTypeName)
    {
        if (IsEnabled())
        {
            WriteEvent(1, schemaTypeName);
        }
    }

    [NonEvent]
    public void SchemaInitialized(Schema schema)
    {
        if (IsEnabled())
        {
            WriteEvent(2, schema.GetType().Name);
        }
    }

    [Event(2, Level = EventLevel.Informational)]
    public void SchemaInitialized(string schemaTypeName)
    {
        if (IsEnabled())
        {
            WriteEvent(2, schemaTypeName);
        }
    }
}
