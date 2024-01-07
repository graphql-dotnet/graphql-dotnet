#if NET5_0_OR_GREATER
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GraphQL.Telemetry;

internal static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds an activity event containing information from the specified exception.
    /// </summary>
    /// <param name="activity">Activity instance.</param>
    /// <param name="ex">Exception to be recorded.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordException(this Activity activity, Exception ex)
    {
        var tagsCollection = new ActivityTagsCollection
        {
            { "exception.type", ex.GetType().FullName },
            { "exception.stacktrace", ex.ToInvariantString() }
        };

        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            tagsCollection.Add("exception.message", ex.Message);
        }

        activity.AddEvent(new ActivityEvent("exception", default, tagsCollection));
    }

    /// <summary>
    /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
    /// appropriate for diagnostics tracing.
    /// </summary>
    /// <param name="exception">Exception to convert to string.</param>
    /// <returns>Exception as string with no culture.</returns>
    public static string ToInvariantString(this Exception exception)
    {
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
#endif
