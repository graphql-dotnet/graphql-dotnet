# Overlapping-fields benchmark results

Final production candidate: `142ecbe6c672e603b72e1742b687dbf06d1584ae`. Original rule: `b3c64151d70e977752c8526c00f13bf2530399f6`. PR 4426: exact head `60fe08bd5007a239ce420c0d049c3e562591b7a3`, whose caches are per visitor despite the older PR description mentioning thread-static caches.

Run on 2026-11-04: Debian 13, AMD EPYC 9V74 (five logical cores visible), .NET SDK 10.0.403, .NET 10.0.14 x64 RyuJIT, Release, BenchmarkDotNet 0.15.8. Each of 76 cases used five warmups and eight measured iterations, a 250 ms target and unroll factor 1. BenchmarkDotNet applied its normal outlier filtering. No other builds/tests ran during measurement.

All implementations ran sequentially in the same process with MemoryDiagnoser. The in-process toolchain avoids the existing multi-target project dependencies overwriting GraphQL.dll variants in BenchmarkDotNet's shared output directory; that failed out-of-process attempt produced no usable measurements and is excluded. Process priority remained normal. Small timing differences should be interpreted cautiously. The committed CSV files expand BenchmarkDotNet's truncated `Inlin(...)00x50 [21]` and `Repea(...)e1000 [21]` query labels to `InlineFragments100x50` and `RepeatedComposite1000`; all measurement fields are unchanged.

[Reproduction command and workloads](README.md), [algorithm/review details](DESIGN.md), and [complete final CSV including errors, standard deviations and GC counts](results-final.csv). Parsing and schema initialization are excluded. Hero, Fragments and Introspection include all validation rules; other cases isolate the overlap rule while retaining standard validation traversal and argument parsing.

## Final comparison

Each cell is **mean microseconds / allocated KiB per validation** (1 KiB = 1,024 bytes).

| Workload | Original | PR 4426 | Proposed |
| --- | ---: | ---: | ---: |
| Hero | 14.55 / 3.75 | 14.33 / 3.44 | 14.00 / 2.18 |
| Fragments | 142.38 / 18.10 | 141.98 / 18.47 | 140.70 / 12.44 |
| Introspection | 290.20 / 32.65 | 303.46 / 41.97 | 287.35 / 17.77 |
| UniqueAliases1000 | 711.54 / 255.31 | 689.74 / 255.00 | 739.21 / 214.52 |
| RepeatedLeaf100 | 282.48 / 124.41 | 50.26 / 13.07 | 38.56 / 1.02 |
| RepeatedLeaf1000 | 23,886.36 / 11,765.17 | 457.45 / 97.47 | 364.49 / 1.03 |
| UnknownFirst100 | 295.81 / 127.43 | 153.04 / 13.77 | 47.87 / 3.64 |
| UnknownFirst1000 | 23,503.52 / 11,789.28 | 10,412.74 / 98.36 | 431.94 / 24.74 |
| LateConflict100 | 414.22 / 277.38 | 427.68 / 301.31 | 391.26 / 279.05 |
| InlineFragments10x10 | 489.47 / 194.39 | 243.85 / 91.56 | 217.96 / 8.03 |
| InlineFragments100x50 | 629,125.95 / 296,293.72 | 12,850.45 / 3,673.19 | 9,457.23 / 252.21 |
| FragmentDiamond8 | 37.14 / 12.01 | 268.26 / 67.80 | 31.42 / 5.11 |
| FragmentDiamond12 | 55.24 / 17.42 | 4,173.76 / 883.07 | 43.29 / 5.11 |
| RepeatedSpread1000 | 485.36 / 3.27 | 607.92 / 74.79 | 495.80 / 1.90 |
| RepeatedComposite1000 | 96,502.75 / 24,080.53 | 1,755.38 / 697.27 | 1,378.88 / 196.31 |
| SharedFragments100 | 362.25 / 123.15 | 1,319.24 / 2,586.60 | 315.48 / 63.60 |
| SharedFragments1000 | 2,824.50 / 989.51 | 25,089.33 / 25,636.74 | 2,550.58 / 431.98 |
| SharedMixed100 | 411.49 / 151.27 | 1,309.93 / 2,599.10 | 360.48 / 63.80 |
| SharedMixed1000 | 3,328.82 / 1,270.77 | 30,331.47 / 25,762.28 | 2,926.01 / 432.16 |

- Introspection allocation falls from 33,430 B (original) and 42,974 B (PR 4426) to 18,198 B: **46% and 58% less**, with similar ordinary-query timings. Hero and Fragments also allocate less than both alternatives.
- The 100 × 50 inline-fragment case falls from **629.13 ms / 303.40 MB** (original) and **12.85 ms / 3.76 MB** (PR) to **9.46 ms / 258 KB**. That is **66.5× faster than original**, **1.36× faster than PR**, and **93.1% fewer bytes than PR**. These MB/KB figures use decimal bytes.
- For 100 → 1,000 repeated leaves, original time grows 84.6×; proposed grows 9.5×. The 1,000-leaf proposed case allocates 1,056 B including the rest of validation.
- With an unknown first type, PR time grows 68.0× for 10× more fields; proposed grows 9.0×. Diamond depth 8 → 12 increases PR time 15.6×; proposed increases 1.4×. These cases expose the reviewed pairwise type scan and repeated fragment-path expansion.
- The proposed implementation is not uniformly faster: UniqueAliases1000 is 3.9% slower than original and 7.2% slower than PR, despite 16% lower allocation. Its dictionary entry array reaches the large-object heap and the run records Gen2 collections. RepeatedSpread1000 is 2.2% slower than original with 42% lower allocation.

## One pass versus a preliminary scan

The `SinglePass` control disables the preliminary leaf scan, so it groups even multi-field sets of plain leaves. The proposed version can accept unaliased, argument-free leaves before creating buffers. Both retain the single-field shortcut, fragment classification and recursive group checks; the control name is not a fixed bound on whole-document visits. Otherwise they share generated code. The full CSV contains all control results.

| Workload | SinglePass µs / KiB | Proposed µs / KiB |
| --- | ---: | ---: |
| Hero | 14.20 / 2.51 | 14.00 / 2.18 |
| Introspection | 287.07 / 18.89 | 287.35 / 17.77 |
| RepeatedLeaf100 | 46.23 / 3.34 | 38.56 / 1.02 |
| RepeatedLeaf1000 | 423.72 / 24.45 | 364.49 / 1.03 |
| InlineFragments100x50 | 9,578.72 / 252.22 | 9,457.23 / 252.21 |

The extra scan reduces the 1,000-leaf case from 25,033 B to 1,056 B and improves its mean from 423.72 µs to 364.49 µs. It also saves buffers on ordinary leaf selections in Hero and Introspection. It does not establish a general timing win for every query; when the scan stops immediately, the two variants have essentially the same work.

## Rejected mixed-fragment candidate

The first timed lazy implementation memoized fragment-only groups but re-expanded every named-fragment field when a local leaf was present. This created a material regression on valid queries. The final implementation instead caches a validated leaf summary and checks each local leaf against it without copying or mutating the summary.

| Workload | Initial candidate µs / KiB | Final candidate µs / KiB |
| --- | ---: | ---: |
| SharedMixed100 | 1,012.21 / 2,138.35 | 360.48 / 63.80 |
| SharedMixed1000 | 9,384.59 / 21,371.35 | 2,926.01 / 432.16 |

For SharedMixed1000 the final candidate reduces the initial candidate's allocation by **98%** and is now faster and smaller than the original algorithm in the final same-run comparison. The initial measurements are retained in [results-initial.csv](results-initial.csv); their `Proposed`/`SinglePass` labels refer to the rejected implementation, not the final commit.

## Limits and correctness

These results do **not** establish unconditional O(document size) validation or DoS immunity. Mixed local composite selections/inline fragments and distinct merge contexts can still require overlapping expansion work. The optimized SharedMixed cases contain local leaves. Argument equality retains the existing implementation. After a proven conflict, diagnostics retain pairwise work, including sparse-failure cases; LateConflict100 is deliberately bounded. Hard worst-case protection requires a separate work/error-budget policy.

Validation accompanying the final candidate: 118 permanent overlap tests passed; 40,000 deterministic differential documents matched the original rule including ordered error messages and locations (17,368 valid, 22,632 invalid). The complete .NET 10 GraphQL.Tests suite passed with 4,452 passed and eight skipped when test collections were serialized. Existing tests mutate process-wide GlobalSwitches, causing unrelated order/parallelism-sensitive failures also observed on the base revision. Formatting and whitespace checks passed. Benchmark setup checked the expected validity of every fixture for all four implementations.

PR 4426 review regressions cover fragment-definition ordering/cache misses; its shared-fragment expansion, unknown-first scan and hash-code-only diagnostic identity are discussed in [DESIGN.md](DESIGN.md).

## Source provenance

The historical source digests are enforced by `prepare-overlapping.py`. Final measured source SHA-256 values:

```text
OverlappingFieldsCanBeMerged.cs
eb69d7ac829f87515446a1025b874c8b8df56cbf841beda94f8af80f95d5be9f
OverlappingFieldsCanBeMerged.FastValidation.cs
20469daf8a74bc791c2ea7fdb198c6338f5549962e8ba5bb11eef2622531b74b
OverlappingFieldsComparisonBenchmark.cs
f3f20e8cc22c8f4e3b39a38d92446f77ff07ce011ea44483c8f0d6cf52c5bad1
```

The rejected initial fast-validation source had SHA-256 `58e90e611fb7335ae63a08d1af46ce76c9e30f397792500691d6d0377af94be0`; it was exploratory and was not committed. Its measured source snapshot and full JSON/log artifacts were retained in the task workspace. Both timed runs used the same machine and settings; the final run took 6 minutes 46 seconds.
