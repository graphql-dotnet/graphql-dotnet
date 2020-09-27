using System;
using System.Threading.Tasks;

namespace GraphQL.Logging
{
    /// <summary>
    /// Interface for recording traces to GraphQL monitoring solutions
    /// </summary>
    public interface ITraceLogger
    {
        /// <summary>
        /// Logs a GraphQL query as a trace for sending to various GraphQL monitoring solutions
        /// </summary>
        /// <param name="start">The time the request was initiated</param>
        /// <param name="operationName">The GraphQL operation name (if provided)</param>
        /// <param name="query">The full GraphQL query</param>
        /// <param name="result">The execution result from the GraphQL middleware</param>
        void LogTrace(DateTime start, string operationName, string query, ExecutionResult result);

        /// <summary>
        /// Used to indicate to the sending service that the size threshold has been reached and send now
        /// </summary>
        AsyncAutoResetEvent ForceSendTrigger { get; }

        /// <summary>
        /// Sends all queued traces to Apollo Studio
        /// </summary>
        /// <returns></returns>
        Task Send();
    }
}
