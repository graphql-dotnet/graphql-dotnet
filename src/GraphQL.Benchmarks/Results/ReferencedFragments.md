# Referenced fragment traversal benchmarks

`ReferencedFragmentsBenchmark` measures `ValidationContext.GetRecursivelyReferencedFragments`
on pre-parsed documents. Each invocation creates a fresh `ValidationContext`, assigns the
same document and operation, and performs the first fragment lookup. Context construction
and result/cache allocations are included; parsing and schema validation are excluded.
There are no cache-hit measurements. Array pools are warmed by BenchmarkDotNet before
measurement, so allocated bytes describe steady-state execution rather than the first
pool rental in a process.

The unchanged baseline is commit `c8c083354e4cfd3ee411c33d7d5a820b05481436` with only the
new benchmark file copied into it. Before and after use the same harness, machine,
runtime, and job settings, run sequentially after builds and tests finished.

## Reproduce

Run this command from each checkout, copying
`src/GraphQL.Benchmarks/Benchmarks/ReferencedFragmentsBenchmark.cs` into the baseline first:

```sh
dotnet run --project src/GraphQL.Benchmarks/GraphQL.Benchmarks.csproj -c Release -- \
  --filter '*ReferencedFragmentsBenchmark*' \
  --launchCount 1 --warmupCount 5 --iterationCount 10 --iterationTime 200 \
  --exporters JSON --artifacts artifacts/referenced-fragments
```

## Workloads

| Scenario | Document |
| --- | --- |
| NoFragments | A normal field selection without fragment definitions; exercises the existing fast path. |
| UnusedFragment | One fragment definition that the operation does not reference. |
| Typical | Three fragments, nested fields, and repeated fragment references. |
| Chain256 | A chain of 256 unique fragments; intentionally shallow enough for the old visitor. |
| Nested128 | 128 alternating field and inline-fragment pairs (256 nested selections), ending in one fragment spread. |
| Wide128 | 128 distinct fragments spread from the operation. |
| Repeated256 | 256 sibling fields, each spreading the same two fragments; one fragment also spreads the other. |
| Directives | Fields, inline fragments, and fragment spreads with literal/variable `@skip` and `@include`; `onlyUsed: true`. |

Deep stack-safety regressions are covered separately by the unit tests; a stack overflow
terminates the old process and cannot produce a useful timing or allocation sample.

## Results

BenchmarkDotNet 0.15.8; Debian GNU/Linux 13 x64; Intel Xeon Platinum 8573C
(5 logical cores available); .NET SDK 10.0.403; .NET 10.0.14, X64 RyuJIT.
One launch, five warmup iterations, ten measurement iterations, 200 ms target
iteration time. BenchmarkDotNet's default outlier filtering applies.
Times are mean ± standard deviation in nanoseconds per operation; allocations are
managed bytes per operation. Speedup is before/after.

| Scenario | Before (ns) | After (ns) | Speedup | Before (B) | After (B) | Allocation reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| NoFragments | 61.48 ± 3.27 | 60.91 ± 3.93 | 1.01× | 408 | 408 | 0.0% |
| UnusedFragment | 281.09 ± 5.80 | 91.28 ± 1.23 | 3.08× | 544 | 544 | 0.0% |
| Typical | 1,254.43 ± 15.19 | 277.29 ± 7.51 | 4.52× | 920 | 632 | 31.3% |
| Chain256 | 394,874.57 ± 7,882.63 | 315,484.12 ± 2,067.65 | 1.25× | 5,096 | 4,808 | 5.7% |
| Nested128 | 22,126.04 ± 140.29 | 2,664.28 ± 11.96 | 8.30× | 824 | 632 | 23.3% |
| Wide128 | 100,826.24 ± 1,865.96 | 86,047.39 ± 2,651.40 | 1.17× | 3,024 | 2,736 | 9.5% |
| Repeated256 | 52,069.59 ± 221.73 | 8,878.75 ± 212.68 | 5.86× | 864 | 632 | 26.9% |
| Directives | 1,469.90 ± 51.10 | 540.54 ± 10.05 | 2.72× | 864 | 632 | 26.9% |

The unchanged no-fragment fast path has the same allocations; its small timing
difference is within measurement variability. The workloads returning fragments allocate
less after the rewrite, with no per-invocation allocation for the warmed traversal
stack. Large sets of distinct fragments still spend time in the existing linear
fragment lookup and duplicate detection.

Raw BenchmarkDotNet summaries: [before CSV](ReferencedFragments.before.csv),
[after CSV](ReferencedFragments.after.csv). Their `Error` column is half of the
99.9% confidence interval, distinct from the standard deviation above.
