using System.Diagnostics;
using OpenTelemetry.Trace;

namespace GraphQL.Tests.Instrumentation;

// Copied from OpenTelemetry.Trace.ActivityHelperExtensions to work with Activity.Status() for .NET5
internal static class ActivityHelperExtensions
{
    private const string UnsetStatusCodeTagValue = "UNSET";
    private const string OkStatusCodeTagValue = "OK";
    private const string ErrorStatusCodeTagValue = "ERROR";

    private static ActivityStatusCode? GetStatusCodeForTagValue(string? statusCodeTagValue)
    {
        return statusCodeTagValue switch
        {
            string _ when UnsetStatusCodeTagValue.Equals(statusCodeTagValue, StringComparison.OrdinalIgnoreCase) => ActivityStatusCode.Unset,
            string _ when ErrorStatusCodeTagValue.Equals(statusCodeTagValue, StringComparison.OrdinalIgnoreCase) => ActivityStatusCode.Error,
            string _ when OkStatusCodeTagValue.Equals(statusCodeTagValue, StringComparison.OrdinalIgnoreCase) => ActivityStatusCode.Ok,
            _ => null,
        };
    }

    private static bool TryGetStatusCodeForTagValue(string? statusCodeTagValue, out ActivityStatusCode statusCode)
    {
        ActivityStatusCode? tempStatusCode = GetStatusCodeForTagValue(statusCodeTagValue);

        statusCode = tempStatusCode ?? default;

        return tempStatusCode.HasValue;
    }

    /// <summary>
    /// Gets the status of activity execution.
    /// Activity class in .NET does not support 'Status'.
    /// This extension provides a workaround to retrieve Status from special tags with key name otel.status_code and otel.status_description.
    /// </summary>
    /// <param name="activity">Activity instance.</param>
    /// <param name="statusCode"><see cref="StatusCode"/>.</param>
    /// <param name="statusDescription">Status description.</param>
    /// <returns><see langword="true"/> if <see cref="Status"/> was found on the supplied Activity.</returns>
    private static bool TryGetStatus(this Activity activity, out ActivityStatusCode statusCode, out string? statusDescription)
    {
        activity.ShouldNotBeNull();

        bool foundStatusCode = false;
        statusCode = default;
        statusDescription = null;

        foreach (ref readonly var tag in activity.EnumerateTagObjects())
        {
            switch (tag.Key)
            {
                case "otel.status_code":
                    foundStatusCode = TryGetStatusCodeForTagValue(tag.Value as string, out statusCode);
                    if (!foundStatusCode)
                    {
                        // If status code was found but turned out to be invalid give up immediately.
                        return false;
                    }

                    break;
                case "otel.status_description":
                    statusDescription = tag.Value as string;
                    break;
                default:
                    continue;
            }

            if (foundStatusCode && statusDescription != null)
            {
                // If we found a status code and a description we break enumeration because our work is done.
                break;
            }
        }

        return foundStatusCode;
    }

    public static ActivityStatusCode Status(this Activity activity)
    {
#if NET6_0_OR_GREATER
        return activity.Status;
#else
        return activity.TryGetStatus(out var statusCode, out string? statusDescription)
            ? statusCode
            : ActivityStatusCode.Unset;
#endif
    }
}
