# Overlapping-fields comparison

This opt-in harness runs four implementations in the same BenchmarkDotNet build,
on the same initialized schemas and parsed documents:

- **Original:** the rule at `b3c64151d70e977752c8526c00f13bf2530399f6`.
- **PR4426:** the rule at `60fe08bd5007a239ce420c0d049c3e562591b7a3`.
- **SinglePass:** the working-tree implementation with only the preliminary
  argument-free, unaliased leaf scan disabled. The single-field shortcut remains.
- **Proposed:** the working-tree implementation, including the preliminary scan.

The preparation script checks SHA-256 digests of the historical sources, then
changes their class/namespace names and shares the production diagnostic DTOs.
Algorithm bodies are unchanged. The internal AST `Print` helper is copied without
changing its behavior. Generated sources live under `obj` and are not committed.
Run preparation again after changing the proposed algorithm so the single-pass
control stays synchronized.

From the repository root, with Python 3 and the .NET SDK on `PATH`:

```sh
python3 src/GraphQL.Benchmarks/Comparisons/prepare-overlapping.py
CompareOverlappingFields=true dotnet run -c Release \
  --project src/GraphQL.Benchmarks -- \
  --filter '*OverlappingFieldsComparisonBenchmark*' \
  --inProcess \
  --warmupCount 5 --iterationCount 8 --iterationTime 250 --unrollFactor 1 \
  --exporters csv json --artifacts BenchmarkDotNet.Artifacts/overlapping-fields
```

`CompareOverlappingFields` enables the comparison classes; a normal benchmark build
excludes them. The in-process toolchain avoids BenchmarkDotNet's shared output
directory colliding with this solution's different GraphQL target frameworks.
The script reads pinned revisions from local Git history if available,
otherwise from GitHub; `--original PATH --pr4426 PATH` supports local snapshots.
Both snapshots are verified even when supplied locally.

Hero, Fragments and Introspection use the complete default validation-rule list,
replacing only the overlapping-fields rule. Other cases isolate that rule.
Validation still performs its standard traversal and argument parsing. Parsing
the document and initializing the schema are excluded from measurements.
Setup checks the expected validity of every fixture for every implementation.

The workloads exercise:

- Small ordinary queries and standard introspection.
- Unique aliases, repeated leaves, repeated composite selections and repeated
  named-fragment spreads.
- 100 and 1,000 distinct aliases selecting the same two named fragments; each
  fragment supplies 50 disjoint response names. This exposes repeated work across
  separate selection sets that represent the same fragment group.
- The same shared-fragment cases with an additional local `x` selection per
  alias. These verify that cached fragment summaries can be checked against
  local leaves without copying or expanding all fragment fields per alias.
- Nested inline fragments at 10 × 10 and 100 × 50 selections.
- Diamond fragment graphs with depth 8 and 12. Each fragment spreads the next one
  twice, making path expansion exponential despite a small input document.
- An unknown type in the first inline fragment, followed by 100 or 1,000 known
  same-name fields. These pass this rule in isolation; other rules diagnose the
  unknown type. This tests representative selection when the first return type
  is unavailable.
- A late conflict after 100 repeated fields, exercising diagnostic generation.
  It is deliberately bounded because producing every conflicting pair is not a
  constant-size output operation.

The single-pass control measures whether a cheap preliminary scan is worthwhile;
it is not a second production implementation. Timing results depend on hardware,
runtime, build configuration and workload. Allocation counts include the rest of
validation, not just the overlapping-fields rule.
