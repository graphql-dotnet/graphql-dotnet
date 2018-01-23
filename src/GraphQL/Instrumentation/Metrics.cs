using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphQL.Instrumentation
{
    public class Metrics : IDisposable
    {
        private readonly bool _enabled;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly ConcurrentBag<PerfRecord> _records = new ConcurrentBag<PerfRecord>();
        private PerfRecord _main;

        public Metrics(bool enabled = true)
        {
            _enabled = enabled;
        }

        public void Start(string operationName)
        {
            if (!_enabled)
            {
                return;
            }

            _main = new PerfRecord("operation", operationName, 0);
            _records.Add(_main);
            _stopwatch.Start();
        }

        public void SetOperationName(string name)
        {
            if (!_enabled)
            {
                return;
            }

            _main.Subject = name;
        }

        public IDisposable Subject(string category, string subject, Dictionary<string, object> metadata = null)
        {
            if (!_enabled)
            {
                return null;
            }

            var record = new PerfRecord(category, subject, _stopwatch.Elapsed.TotalMilliseconds, metadata);
            _records.Add(record);
            return new Marker(record, _stopwatch);
        }

        public IEnumerable<PerfRecord> AllRecords => _records.Where(x => x != null).OrderBy(x => x.Start).ToArray();

        public IEnumerable<PerfRecord> Finish()
        {
            if (!_enabled)
            {
                return null;
            }

            _main?.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
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
                _record.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        public void Dispose()
        {
            if (_stopwatch.IsRunning) _stopwatch.Stop();
        }
    }
}
