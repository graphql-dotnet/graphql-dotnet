using System.Diagnostics.Tracing;

namespace GraphQL.Tests.Instrumentation;

internal class TestEventListener : EventListener
{
    public List<EventWrittenEventArgs> Events { get; } = new();

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "GraphQL-NET")
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        Events.Add(eventData);
    }
}
