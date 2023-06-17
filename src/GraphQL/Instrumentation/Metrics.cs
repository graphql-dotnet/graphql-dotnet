using GraphQLParser;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Records metrics during execution of a GraphQL document.
    /// </summary>
    public class Metrics
    {
        private ValueStopwatch _stopwatch;
        private readonly List<PerfRecord>? _records;
        private PerfRecord? _main;

        /// <summary>
        /// Gets an instance of the metrics for which metrics collection is disabled.
        /// </summary>
        public static Metrics None { get; } = new Metrics(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class.
        /// </summary>
        /// <param name="enabled">Indicates if metrics should be recorded for this execution.</param>
        public Metrics(bool enabled = true)
        {
            Enabled = enabled;
            if (enabled)
                _records = new List<PerfRecord>();
        }

        /// <summary>
        /// Shows whether metrics collection is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Logs the start of the execution.
        /// </summary>
        /// <param name="operationName">The name of the GraphQL operation.</param>
        public Metrics Start(string? operationName)
        {
            if (Enabled)
            {
                if (_main != null)
                    throw new InvalidOperationException("Metrics.Start has already been called");

                _main = new PerfRecord("operation", operationName, 0);
                _records!.Add(_main);
                _stopwatch = ValueStopwatch.StartNew();
            }

            return this;
        }

        /// <summary>
        /// Sets the name of the GraphQL operation.
        /// </summary>
        public Metrics SetOperationName(ROM name)
        {
            if (Enabled && _main != null)
                _main.Subject = name.IsEmpty ? null : (string)name;

            return this;
        }

        /// <summary>
        /// Records an performance metric.
        /// </summary>
        /// <param name="category">The category name for recorded metric, for example, "document" or "field".</param>
        /// <param name="subject">The subject for recorded metric, for example, "Initializing schema" or "Building document".</param>
        /// <param name="metadata">A dictionary of additional metadata for metric.</param>
        /// <returns><see cref="Marker"/></returns>
        public Marker Subject(string category, string? subject, Dictionary<string, object?>? metadata = null)
        {
            if (!Enabled)
                return Marker.Empty;

            if (_main == null)
                throw new InvalidOperationException("Metrics.Start should be called before calling Metrics.Subject");

            var record = new PerfRecord(category, subject, _stopwatch.Elapsed.TotalMilliseconds, metadata);
            lock (_records!)
                _records!.Add(record);
            return new Marker(record, _stopwatch);
        }

        /// <summary>
        /// Returns the collected performance metrics.
        /// </summary>
        public PerfRecord[]? Finish()
        {
            if (!Enabled)
                return null;

            _main?.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
            return _records!.OrderBy(x => x.Start).ToArray();
        }

        /// <summary>
        /// An object that, when disposed, records the completion time of a performance metric in an assigned <see cref="PerfRecord"/>.
        /// </summary>
        public readonly struct Marker : IDisposable
        {
            private readonly PerfRecord _record;
            private readonly ValueStopwatch _stopwatch;

            /// <summary>
            /// Returns an instance that does not perform any action when disposed.
            /// </summary>
            public static readonly Marker Empty;

            /// <summary>
            /// Initializes an instance with the specified performance metric and running stopwatch.
            /// </summary>
            public Marker(PerfRecord record, ValueStopwatch stopwatch)
            {
                _record = record;
                _stopwatch = stopwatch;
            }

            /// <summary>
            /// Stores the completion time of the assigned performance metric.
            /// </summary>
            public void Dispose()
            {
                _record?.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
