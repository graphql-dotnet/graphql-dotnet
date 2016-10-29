using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphQL.Instrumentation
{
    public class Timings : IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly IList<PerfRecord> _records = new List<PerfRecord>();
        private PerfRecord _main;

        public void Start(string operationName)
        {
            _main = new PerfRecord("operation", operationName, 0);
            _records.Add(_main);
            _stopwatch.Start();
        }

        public void SetOperationName(string name)
        {
            _main.Subject = name;
        }

        public IDisposable Subject(string type, string subject)
        {
            var record = new PerfRecord(type, subject, _stopwatch.ElapsedMilliseconds);
            _records.Add(record);
            return new Marker(record, _stopwatch);
        }

        public IEnumerable<PerfRecord> AllRecords => _records.OrderBy(x => x.Start).ToArray();

        public IEnumerable<PerfRecord> Finish()
        {
            _main?.MarkEnd(_stopwatch.ElapsedMilliseconds);
            _stopwatch.Stop();
            return AllRecords;
        }

        public class Marker : IDisposable
        {
            private readonly PerfRecord _record;
            private readonly Stopwatch _stopwatch;

            public Marker(PerfRecord record, Stopwatch stopwatch)
            {
                _record = record;
                _stopwatch = stopwatch;
            }

            public void Dispose()
            {
                _record.MarkEnd(_stopwatch.ElapsedMilliseconds);
            }
        }

        public void Dispose()
        {
            if (_stopwatch.IsRunning) _stopwatch.Stop();
        }
    }
}
