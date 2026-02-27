# Samples

This repository includes multiple runnable examples. Use them as reference implementations for setup patterns, schema styles, and project structure.

## Sample index

| Sample | Focus | Path |
| --- | --- | --- |
| GraphQL.ValidationRules.Sample | Custom validation rule with `ValidationRuleBase` and `DocumentValidator.CoreRules` | `samples/GraphQL.ValidationRules.Sample` |
| GraphQL.AotCompilationSample.CodeFirst | Native AOT setup with code-first schema | `samples/GraphQL.AotCompilationSample.CodeFirst` |
| GraphQL.AotCompilationSample.TypeFirst | Native AOT setup with type-first schema | `samples/GraphQL.AotCompilationSample.TypeFirst` |
| GraphQL.StarWars | Code-first StarWars API sample | `src/GraphQL.StarWars` |
| GraphQL.StarWars.TypeFirst | Type-first StarWars API sample | `src/GraphQL.StarWars.TypeFirst` |
| GraphQL.Harness | Full-featured ASP.NET Core host with auth, subscriptions, and tooling | `samples/GraphQL.Harness` |
| GraphQL.DataLoader.Sample.Default | DataLoader usage without DI container integration | `samples/GraphQL.DataLoader.Sample.Default` |
| GraphQL.DataLoader.Sample.DI | DataLoader usage with DI container integration | `samples/GraphQL.DataLoader.Sample.DI` |
| GraphQL.Federation.SchemaFirst.Sample1 | Federation sample using schema-first approach | `samples/GraphQL.Federation.SchemaFirst.Sample1` |
| GraphQL.Federation.SchemaFirst.Sample2 | Federation sample using schema-first approach (alternate setup) | `samples/GraphQL.Federation.SchemaFirst.Sample2` |
| GraphQL.Federation.CodeFirst.Sample3 | Federation sample using code-first approach | `samples/GraphQL.Federation.CodeFirst.Sample3` |
| GraphQL.Federation.TypeFirst.Sample4 | Federation sample using type-first approach | `samples/GraphQL.Federation.TypeFirst.Sample4` |

## Running a sample

Run any sample from the repository root:

```bash
dotnet run --project <path-to-sample-csproj>
```

Examples:

```bash
dotnet run --project samples/GraphQL.ValidationRules.Sample/GraphQL.ValidationRules.Sample.csproj
dotnet run --project samples/GraphQL.AotCompilationSample.CodeFirst/GraphQL.AotCompilationSample.CodeFirst.csproj
dotnet run --project samples/GraphQL.Harness/GraphQL.Harness.csproj
```
