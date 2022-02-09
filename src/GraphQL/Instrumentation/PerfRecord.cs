using System.Diagnostics;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// Records a performance metric.
    /// </summary>
    [DebuggerDisplay("Type={Category} Subject={Subject} Duration={Duration}")]
    public class PerfRecord
    {
        /// <summary>
        /// Initializes a new instance with the specified properties.
        /// </summary>
        public PerfRecord(string category, string? subject, double start, Dictionary<string, object?>? metadata = null)
        {
            Category = category;
            Subject = subject;
            Start = start;
            Metadata = metadata;
        }

        /// <summary>
        /// Sets the completion time, represented as an offset in milliseconds from starting the GraphQL operation's execution.
        /// </summary>
        public void MarkEnd(double end) => End = end;

        /// <summary>
        /// Gets or sets the category name.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the subject name.
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of additional metadata.
        /// </summary>
        public Dictionary<string, object?>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the start time, represented as an offset in milliseconds from starting the GraphQL operation's execution.
        /// </summary>
        public double Start { get; set; }

        /// <summary>
        /// Gets or sets the completion time, represented as an offset in milliseconds from starting the GraphQL operation's execution.
        /// </summary>
        public double End { get; set; }

        /// <summary>
        /// Returns the total number of milliseconds required to execute the operation represented by this performance metric.
        /// </summary>
        public double Duration => End - Start;

        /// <summary>
        /// Returns metadata for the specified key. Similar to <see cref="Metadata"/>[<paramref name="key"/>], but returns <c>default</c>
        /// if <see cref="Metadata"/> is <c>null</c> or the specified key does not exist.
        /// </summary>
        public T? MetaField<T>(string key)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var value) ? (T?)value : default;
        }
    }
}
