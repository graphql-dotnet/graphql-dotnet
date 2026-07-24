# Samples

This page tracks runnable examples in the repository.

## Core samples

| Sample | Approach | Path |
| --- | --- | --- |
| GraphQL.SchemaFirst.Sample | Schema-first (`Schema.For`) | `samples/GraphQL.SchemaFirst.Sample` |
| GraphQL.AotCompilationSample.CodeFirst | Code-first AOT | `samples/GraphQL.AotCompilationSample.CodeFirst` |
| GraphQL.AotCompilationSample.TypeFirst | Type-first AOT | `samples/GraphQL.AotCompilationSample.TypeFirst` |
| GraphQL.StarWars | Code-first | `src/GraphQL.StarWars` |
| GraphQL.StarWars.TypeFirst | Type-first | `src/GraphQL.StarWars.TypeFirst` |
| GraphQL.Harness | Full server sample | `samples/GraphQL.Harness` |

## Running samples

From repository root:

```bash
dotnet run --project <path-to-csproj>
```

Examples:

```bash
dotnet run --project samples/GraphQL.SchemaFirst.Sample/GraphQL.SchemaFirst.Sample.csproj
dotnet run --project samples/GraphQL.AotCompilationSample.CodeFirst/GraphQL.AotCompilationSample.CodeFirst.csproj
dotnet run --project samples/GraphQL.Harness/GraphQL.Harness.csproj
```
