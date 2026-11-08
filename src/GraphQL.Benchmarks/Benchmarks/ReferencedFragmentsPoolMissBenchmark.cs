using System.Buffers;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using GraphQLParser.AST;

namespace GraphQL.Benchmarks;

// Each iteration has exactly one traversal, with all required pool buckets emptied
// immediately beforehand. Setup allocations and time are excluded by BenchmarkDotNet.
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 10, iterationCount: 100, invocationCount: 1)]
public class ReferencedFragmentsPoolMissBenchmark
{
    private readonly ReferencedFragmentsBenchmark _benchmark = new() { Scenario = "DeepBranching257" };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _benchmark.GlobalSetup();

        // Initialize the pool infrastructure even when running the recursive baseline,
        // which does not use this pool. Keep infrastructure initialization out of results.
        var pool = ArrayPool<(List<ASTNode>, int)>.Shared;
        for (int size = 16; size <= 512; size *= 2)
            pool.Return(pool.Rent(size), clearArray: true);

        // Single-invocation measurements do not provide the usual throughput warmup.
        for (int i = 0; i < 10_000; ++i)
            _benchmark.FindFragments();
    }

    [IterationSetup]
    public void EmptyTraversalPool()
    {
        var pool = ArrayPool<(List<ASTNode>, int)>.Shared;
        for (int size = 16; size <= 512; size *= 2)
        {
            // Drain both the thread-local and shared caches, stopping when Rent itself
            // allocates a new array. This dedicated benchmark process has no other users
            // of the exact tuple pool. Never return the drained arrays.
            while (true)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                var buffer = pool.Rent(size);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                GC.KeepAlive(buffer);
                if (allocated >= buffer.Length * Unsafe.SizeOf<(List<ASTNode>, int)>())
                    break;
            }
        }
    }

    [Benchmark]
    public List<GraphQLFragmentDefinition> FindFragments() => _benchmark.FindFragments();
}
