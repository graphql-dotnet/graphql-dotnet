using System.Diagnostics;

namespace GraphQL.Instrumentation
{
    /// <summary>
    /// This is already familiar <see cref="Stopwatch"/> but as a readonly struct. Doesn't allocate memory on the managed heap.
    /// </summary>
    public readonly struct ValueStopwatch
    {
        private static readonly double _timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private readonly long _startTimestamp;

        /// <inheritdoc cref="Stopwatch.IsRunning"/>
        public bool IsActive => _startTimestamp != 0;

        private ValueStopwatch(long startTimestamp)
        {
            _startTimestamp = startTimestamp;
        }

        /// <inheritdoc cref="Stopwatch.StartNew"/>
        public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

        /// <inheritdoc cref="Stopwatch.Elapsed"/>
        public TimeSpan Elapsed
        {
            get
            {
                // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
                // So it being 0 is a clear indication of default(ValueStopwatch)
                if (!IsActive)
                    throw new InvalidOperationException("An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time.");

                long end = Stopwatch.GetTimestamp();
                long timestampDelta = end - _startTimestamp;
                long ticks = (long)(_timestampToTicks * timestampDelta);
                return new TimeSpan(ticks);
            }
        }
    }
}
