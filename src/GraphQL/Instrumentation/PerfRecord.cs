using System.Diagnostics;

namespace GraphQL.Instrumentation
{
    [DebuggerDisplay("Type={Type} Subject={Subject} Duration={Duration}")]
    public class PerfRecord
    {
        public PerfRecord(string type, string subject, long start)
        {
            Type = type;
            Subject = subject;
            Start = start;
        }

        public void MarkEnd(long end)
        {
            End = end;
        }

        public string Type { get; }

        public string Subject { get; set; }

        public long Start { get; }

        public long End { get; private set; }

        public long Duration => End - Start;
    }
}
