using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Logging;
using GraphQL.Types;
using GraphQL.Utilities;
using mdg.engine.proto;
using Microsoft.AspNetCore.Http;
using ProtoBuf;

namespace GraphQL.ApolloStudio
{
    /// <summary>
    /// Records GraphQL execution results to Apollo Studio
    /// </summary>
    public class ApolloReportingTraceLogger : ITraceLogger
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _apiKey;
        private readonly ReportHeader _reportHeader;
        private ConcurrentDictionary<string, TracesAndStats> _traces = new ConcurrentDictionary<string, TracesAndStats>();
        private readonly MetricsToTraceConverter _metricsToTraceConverter = new MetricsToTraceConverter();
        // Send batches at 2mb so we stay well below the 4mb limit recommended
        private const int BATCH_THRESHOLD_SIZE = 2 * 1024 * 1024;

        /// <summary>
        /// Creates a new Apollo Reporting trace logger for sending queries to Apollo Studio
        /// </summary>
        /// <param name="httpClientFactory">HttpClient factory from DI</param>
        /// <param name="httpContextAccessor">HttpContext accessor for the current request (for headers)</param>
        /// <param name="schema">The current GraphQL schema</param>
        /// <param name="apiKey">Apollo Studio API Key</param>
        /// <param name="schemaTag">Schema tag (usually indicates environment)</param>
        public ApolloReportingTraceLogger(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ISchema schema, string apiKey, string schemaTag)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _apiKey = apiKey;
            _reportHeader = new ReportHeader
            {
                Hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? Environment.MachineName,
                AgentVersion = "engineproxy 0.1.0",
                ServiceVersion = Assembly.GetExecutingAssembly().FullName,
                RuntimeVersion = $".NET Core {Environment.Version}",
                Uname = Environment.OSVersion.ToString(),
                SchemaTag = schemaTag,
                ExecutableSchemaId = ComputeSha256Hash(new SchemaPrinter(schema).Print())
            };
        }

        /// <summary>
        /// Logs a GraphQL query as a trace for sending to Apollo Studio
        /// </summary>
        /// <param name="start">The time the request was initiated</param>
        /// <param name="operationName">The GraphQL operation name (if provided)</param>
        /// <param name="query">The full GraphQL query</param>
        /// <param name="result">The execution result from the GraphQL middleware</param>
        public void LogTrace(DateTime start, string operationName, string query, ExecutionResult result)
        {
            var trace = _metricsToTraceConverter.CreateTrace(result, start);

            if (trace != null)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userAgent = (httpContext.Request.Headers.TryGetValue("User-Agent", out var userAgentHeader) ? userAgentHeader.ToString() : "Unknown/Unknown").Split('/');
                trace.ClientName = httpContext.Request.Headers.TryGetValue("apollographql-client-name", out var clientName) ? clientName.ToString() : userAgent.First();
                trace.ClientVersion = httpContext.Request.Headers.TryGetValue("apollographql-client-version", out var clientVersion) ? clientVersion.ToString() : userAgent.Last();

                var tracesAndStats = _traces.GetOrAdd($"# ${(string.IsNullOrWhiteSpace(operationName) ? "-" : operationName)}\n{MinimalWhitespace(query)}",
                    key => new TracesAndStats());
                tracesAndStats.Traces.Add(trace);

                // Trigger sending now if we exceed the batch threshold size (2mb)
                if (Serializer.Measure(CreateReport(_traces)).Length > BATCH_THRESHOLD_SIZE)
                    ForceSendTrigger.Set();
            }
        }

        /// <summary>
        /// Used to indicate to the sending service that the size threshold has been reached and send now
        /// </summary>
        public AsyncAutoResetEvent ForceSendTrigger { get; } = new AsyncAutoResetEvent();

        /// <summary>
        /// Sends all queued traces to Apollo Studio
        /// </summary>
        /// <returns></returns>
        public async Task Send()
        {
            // Swap values atomically so we don't get an add after we retrieve and before we clear
            IDictionary<string, TracesAndStats> traces = Interlocked.Exchange(ref _traces, new ConcurrentDictionary<string, TracesAndStats>());
            if (traces.Count > 0)
            {
                var report = CreateReport(traces);

                byte[] bytes;
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Fastest))
                        Serializer.Serialize(gzipStream, report);
                    bytes = memoryStream.ToArray();
                }

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri("https://engine-report.apollodata.com/api/ingress/traces"));
                httpRequestMessage.Headers.Add("X-Api-Key", _apiKey);

                httpRequestMessage.Content = new ByteArrayContent(bytes)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/protobuf"),
                        ContentEncoding = {"gzip"}
                    }
                };

                var client = _httpClientFactory.CreateClient();
                await client.SendAsync(httpRequestMessage);
            }
        }

        private static string MinimalWhitespace(string requestQuery) => Regex.Replace(requestQuery.Trim().Replace("\r", "\n").Replace("\n", " "), @"\s{2,}", " ");

        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using var sha256Hash = SHA256.Create();
            // ComputeHash - returns byte array  
            var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert byte array to a string   
            var builder = new StringBuilder();
            foreach (byte t in bytes)
                builder.Append(t.ToString("x2"));
            return builder.ToString();
        }

        private Report CreateReport(IDictionary<string, TracesAndStats> traces)
        {
            var report = new Report
            {
                Header = _reportHeader
            };

            foreach (var trace in traces)
                report.TracesPerQueries.Add(trace.Key, trace.Value);

            return report;
        }
    }
}
