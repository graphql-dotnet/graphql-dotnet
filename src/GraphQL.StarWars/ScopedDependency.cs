using System;
using System.Diagnostics;
using System.Threading;

namespace GraphQL.StarWars
{
    [DebuggerDisplay("ScopedDependency {Index}")]
    public class ScopedDependency : IDisposable
    {
        private static int _counter;

        public ScopedDependency()
        {
            Index = Interlocked.Increment(ref _counter);
            Console.WriteLine($"ScopedDependency {Index} created");
        }

        public int Index { get; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            Console.WriteLine($"ScopedDependency {Index} disposed");
        }
    }

    [DebuggerDisplay("ScopedOtherDependency {Index}")]
    public class ScopedOtherDependency : IDisposable
    {
        private static int _counter;

        public ScopedOtherDependency()
        {
            Index = Interlocked.Increment(ref _counter);
            Console.WriteLine($"ScopedOtherDependency {Index} created");
        }

        public int Index { get; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            Console.WriteLine($"ScopedOtherDependency {Index} disposed");
        }
    }
}
