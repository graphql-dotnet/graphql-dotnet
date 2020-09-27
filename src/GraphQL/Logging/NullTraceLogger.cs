using System;
using System.Threading.Tasks;

namespace GraphQL.Logging
{
    /// <summary>
    /// This class needs to be replaced by a functional instance of ITraceLogger in order to log traces
    /// </summary>
    public class NullTraceLogger : ITraceLogger
    {
        /// <summary>
        /// Does a whole lot of nothing (default behaviour)
        /// </summary>
        /// <param name="operationName">The GraphQL operation name (if provided)</param>
        /// <param name="start">The time the request was initiated</param>
        /// <param name="query">The full GraphQL query</param>
        /// <param name="result">The execution result from the GraphQL middleware</param>
        public void LogTrace(DateTime start, string operationName, string query, ExecutionResult result)
        {
            // This class needs to be replaced by a functional instance of IGraphQLTraceLogger in order to log traces
        }

        /// <summary>
        /// Used to indicate to the sending service that the size threshold has been reached and send now
        /// </summary>
        public AsyncAutoResetEvent ForceSendTrigger { get; } = new AsyncAutoResetEvent();

        /// <summary>
        /// Sends all queued traces to Apollo Studio (none for this case)
        /// </summary>
        /// <returns></returns>
        public Task Send() => Task.CompletedTask;
    }
}
