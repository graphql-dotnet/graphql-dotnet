using System.Diagnostics.Tracing;
using System.Globalization;

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

    [Event(1, Level = EventLevel.Verbose)]
    public void RequestIsFilteredOut()
    {
        WriteEvent(1);
    }

    [NonEvent]
    public void RequestFilterException(Exception ex)
    {
        if (IsEnabled(EventLevel.Error, EventKeywords.All))
        {
            RequestFilterException(ex.ToInvariantString());
        }
    }

    [Event(2, Message = "Filter threw exception, request will not be traced.", Level = EventLevel.Error)]
    public void RequestFilterException(string exception)
    {
        WriteEvent(2, exception);
    }

    [Event(3, Level = EventLevel.Informational, Keywords = Keywords.Schema)]
    public void SchemaCreated(string schemaTypeName)
    {
        if (IsEnabled(EventLevel.Informational, Keywords.Schema))
        {
            WriteEvent(3, schemaTypeName);
        }
    }

    [Event(4, Level = EventLevel.Informational, Keywords = Keywords.Schema)]
    public void SchemaInitialized(string schemaTypeName)
    {
        if (IsEnabled(EventLevel.Informational, Keywords.Schema))
        {
            WriteEvent(4, schemaTypeName);
        }
    }
}

// Copied from https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Contrib.Shared/Api/ExceptionExtensions.cs
internal static class ExceptionExtensions
{
    /// <summary>
    /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
    /// appropriate for diagnostics tracing.
    /// </summary>
    /// <param name="exception">Exception to convert to string.</param>
    /// <returns>Exception as string with no culture.</returns>
    public static string ToInvariantString(this Exception exception)
    {
        var originalUICulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
        }
    }
}
