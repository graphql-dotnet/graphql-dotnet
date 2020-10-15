``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17134.1184 (1803/April2018Update/Redstone4)
Intel Core i5-6200U CPU 2.30GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
Frequency=2343755 Hz, Resolution=426.6658 ns, Timer=TSC
.NET Core SDK=3.0.100
  [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X86 RyuJIT
  DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X86 RyuJIT

```
Baseline:

|        Method |        Mean |     Error |    StdDev |      Median |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------------:|----------:|----------:|------------:|---------:|--------:|------:|----------:|
| Introspection | 4,551.45 us | 80.492 us | 75.292 us | 4,569.91 us | 320.3125 | 93.7500 |     - | 959.38 KB |
|          Hero |    83.63 us |  1.659 us |  3.910 us |    82.08 us |  11.2305 |       - |     - |  17.26 KB |

Heap allocation optimizations #1456:

|        Method |        Mean |     Error |    StdDev |      Median |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------------:|----------:|----------:|------------:|---------:|--------:|------:|----------:|
| Introspection | 3,308.98 us | 31.510 us | 26.312 us | 3,303.90 us | 261.7188 | 78.1250 |     - | 792.49 KB |
|          Hero |    72.85 us |  1.826 us |  5.239 us |    70.34 us |   9.7656 |       - |     - |  15.06 KB |

New baseline 43a80a8:

|        Method |        Mean |     Error |    StdDev |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------------:|----------:|----------:|---------:|--------:|------:|----------:|
| Introspection | 3,116.01 us | 23.753 us | 22.218 us | 265.6250 | 74.2188 |     - | 736.69 KB |
|          Hero |    76.57 us |  0.653 us |  0.510 us |  10.8643 |       - |     - |  16.77 KB |

Heap allocation optimizations #1456:

|        Method |        Mean |     Error |    StdDev |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------------:|----------:|----------:|---------:|--------:|------:|----------:|
| Introspection | 2,665.72 us | 15.886 us | 12.403 us | 203.1250 | 58.5938 |     - | 605.74 KB |
|          Hero |    62.84 us |  0.462 us |  0.410 us |   9.5215 |       - |     - |  14.61 KB |

GraphQL-Parser 5.0.1:

|        Method |        Mean |     Error |    StdDev |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------------:|----------:|----------:|---------:|--------:|------:|----------:|
| Introspection | 2,517.18 us | 34.878 us | 32.625 us | 183.5938 | 58.5938 |     - | 594.49 KB |
|          Hero |    54.87 us |  0.696 us |  0.581 us |   8.9722 |       - |     - |  13.84 KB |
