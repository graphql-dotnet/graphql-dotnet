```

BenchmarkDotNet v0.15.2, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.5 (10.0.526.15411), X64 RyuJIT AVX2


```
| Method        | Mean | Error |
|-------------- |-----:|------:|
| Introspection |   NA |    NA |
| Fragments     |   NA |    NA |
| Hero          |   NA |    NA |

Benchmarks with issues:
  ValidationBenchmark.Introspection: DefaultJob
  ValidationBenchmark.Fragments: DefaultJob
  ValidationBenchmark.Hero: DefaultJob
