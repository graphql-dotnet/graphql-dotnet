using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Instrumentation
{
    public class Metrics
    {
        private readonly bool _enabled;
        private ValueStopwatch _stopwatch;
        private readonly List<PerfRecord> _records;
        private PerfRecord _main;

        public Metrics(bool enabled = true)
        {
            _enabled = enabled;
            if (enabled)
                _records = new List<PerfRecord>();
        }

        public Metrics Start(string operationName)
        {
            if (_enabled)
            {
                if (_main != null)
                    throw new InvalidOperationException("Metrics.Start has already been called");

                _main = new PerfRecord("operation", operationName, 0);
                _records.Add(_main);
                _stopwatch = ValueStopwatch.StartNew();
            }

            return this;
        }

        public Metrics SetOperationName(string name)
        {
            if (_enabled && _main != null)
                _main.Subject = name;

            return this;
        }

        public Marker Subject(string category, string subject, Dictionary<string, object> metadata = null)
        {
            if (!_enabled)
                return Marker.Empty;

            if (_main == null)
                throw new InvalidOperationException("Metrics.Start should be called before calling Metrics.Subject");

            var record = new PerfRecord(category, subject, _stopwatch.Elapsed.TotalMilliseconds, metadata);
            lock (_records)
                _records.Add(record);
            return new Marker(record, _stopwatch);
        }

        public PerfRecord[] Finish()
        {
            if (!_enabled)
                return null;

            _main?.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
            return _records.OrderBy(x => x.Start).ToArray();
        }

        public readonly struct Marker : IDisposable
        {
            private readonly PerfRecord _record;
            private readonly ValueStopwatch _stopwatch;

            public static readonly Marker Empty;

            public Marker(PerfRecord record, ValueStopwatch stopwatch)
            {
                _record = record;
                _stopwatch = stopwatch;
            }

            public void Dispose()
            {
                if (_record != null)
                    _record.MarkEnd(_stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
