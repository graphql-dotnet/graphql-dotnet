# Lazy overlap validation

The production rule first proves compatibility and constructs diagnostics only
after finding a mismatch. The existing pairwise diagnostic implementation is
retained to preserve error ordering, nested reasons and source locations.

## Passes and node types

The normal document visitor checks each field/operation selection set and every
unused fragment definition. Inline fragments belong to their containing set;
checking them independently would repeat the same work at each nesting level.

A short first scan accepts single fields and sets containing only unaliased,
argument-free leaves. Repeating such a leaf on the same parent cannot introduce
a conflict. Descendant field selections still receive their own validation.
This scan creates no collections. If it cannot prove compatibility, grouping
performs the substantive check. The benchmark's `SinglePass` control disables
the leaf scan while retaining the single-field shortcut.

The grouped check stores the first response name inline and creates a dictionary
only on a second name. Each dictionary entry contains one field representative
and optional collision state. Repeated leaves on the same parent need no list
or collision object. A child-selection list is created only when distinct
composite selections must merge. A large struct containing all possible state
in every dictionary slot was also tried; allocation measurements rejected it.

Output shapes use the first **known** return type as their representative.
Name/argument checks use one representative per concrete parent, introducing a
parent dictionary only when distinct concrete parents occur. An abstract parent
must agree with all concrete representatives; once present, it can represent
all subsequent name/argument checks.

Shape and name checks run together whenever possible. Distinct concrete parents
require separate checks: shapes must agree across the whole group, while names
and arguments are checked in each concrete group plus abstract fields. This
keeps inherited mutual exclusion when descendants share the same return type.

Named fragments are indexed on first use and visited once per expanded group,
including paths through a shared fragment graph. Completed fragment-only groups
are cached independent of spread order and duplicates. Recursive merged groups
are memoized by their actual source sets and check kind. All caches are local
to one validation and are created on demand.

Mixed selections containing local leaves and named spreads reuse an immutable
fragment summary. Each local leaf is compared with the summary's matching
response-name representatives; local leaves are also checked against one
another. A leaf introduces no child selections, so this avoids copying and
rechecking an entire fragment map under each alias. The first benchmarked
candidate lacked this cache and regressed substantially on `SharedMixed1000`;
that result prompted the extra cache and its correctness tests.

## Review of PR 4426

The comparison pins head `60fe08bd5007a239ce420c0d049c3e562591b7a3`:

- Removing a fragment from the visited set when expansion returns traverses
  shared fragments once per path, producing exponential expansion on diamonds.
- A missing first field type sends the supposedly quick type check through a
  nested pairwise loop. The unknown-first benchmark measures this behavior.
- Diagnostic deduplication stores runtime hash codes as node identities.
  Distinct node references can collide and suppress an error.
- Merging silently skips child selections not yet in the expansion cache.
  The unused-fragment reproducer returns zero errors when `Outer` precedes
  `Inner`, and one error when the definitions are reversed. Both the original
  and candidate reject both orders, with or without an unrelated operation.

The candidate avoids the latter two mechanisms: it loads selections directly,
uses source-set equality for memoization, and delegates diagnostics to the
original implementation.

## Complexity boundary

Compatible response-name groups use representative comparisons rather than all
field pairs. Shared fragments are not expanded once per route. The benchmarks
measure scaling for repeated leaves, composites, inline fragments, fragment
diamonds, unknown types and reused fragment groups.

This is **not an unconditional O(document size) or DoS-immunity claim**. Work can
still depend on the total expanded selections across distinct merge contexts.
Mixed local composite selections or inline fragments still use the full merge
path and do not share the local-leaf summary shortcut. Distinct fragment groups
can also require overlapping expansions. The `SharedMixed` benchmarks cover
local leaves, not every possible mixed composite selection.
Argument equality also retains the existing implementation. Once a mismatch is
found, diagnostics retain pairwise work, including cases with sparse errors.
Hard worst-case validation limits require a separate work/error-budget policy.

## Validation

The existing overlap tests retain their original assertions. Added regressions
cover aliases, argument values/order, wrappers, abstract and concrete parents,
inherited exclusion, unknown types/fields, fragment order, unused fragments,
cycles, validator reuse and a depth-30 shared fragment graph. A temporary
deterministic differential harness also compared 40,000 documents against the
original rule, including full ordered error messages and locations; all matched
(17,368 valid and 22,632 invalid). This includes 10,000 cases specifically
exercising the mixed local-leaf/fragment-summary path.

The final .NET 10 Release run passed all 118 focused overlap tests. The complete
`GraphQL.Tests` project passed 4,452 tests with eight skips when test collections
ran sequentially. Parallel runs encountered existing tests that change
process-wide `GlobalSwitches`; failures also reproduced on the untouched base.
The optional settings file below affects only the explicit local test command,
not repository-wide or CI configuration:

```sh
dotnet test src/GraphQL.Tests/GraphQL.Tests.csproj -c Release -f net10.0 \
  -p:SingleTestPlatform=true \
  --settings src/GraphQL.Benchmarks/Comparisons/serial-tests.runsettings
```

The library was built for its existing target frameworks (`netstandard2.0`,
`netstandard2.1`, `net5.0`, `net6.0`); tests ran on .NET 10. Formatting checks
passed for the changed source files.

See [the reproducible harness](README.md) and [measured results](RESULTS.md).
