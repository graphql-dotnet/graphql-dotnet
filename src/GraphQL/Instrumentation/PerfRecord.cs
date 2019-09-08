using System.Collections.Generic;
using System.Diagnostics;

namespace GraphQL.Instrumentation
{
    [DebuggerDisplay("Type={Category} Subject={Subject} Duration={Duration}")]
    public class PerfRecord
    {
        public PerfRecord(string category, string subject, double start, Dictionary<string, object> metadata = null)
        {
            Category = category;
            Subject = subject;
            Start = start;
            Metadata = metadata;
        }

        public void MarkEnd(double end) => End = end;

        public string Category { get; set; }

        public string Subject { get; set; }

        public Dictionary<string, object> Metadata { get; set; }

        public double Start { get; set; }

        public double End { get; set; }

        public double Duration => End - Start;

        public T MetaField<T>(string key)
        {
            var local = Metadata;
            return local != null && local.TryGetValue(key, out var value) ? (T)value : default;
        }
    }
}
