# Samples

GraphQL.NET includes small sample projects that show supported setup patterns for the core library. Use these projects when you want runnable code in addition to the documentation pages.

## Core Library Samples

| Sample | Focus | Path |
| --- | --- | --- |
| Custom validation rule | Field metadata and request-context based validation with `ValidationRuleBase` | `samples/GraphQL.CustomValidationRule.Sample` |
| AOT compilation, code-first | Native AOT setup with a code-first schema | `samples/GraphQL.AotCompilationSample.CodeFirst` |
| AOT compilation, type-first | Native AOT setup with a type-first schema | `samples/GraphQL.AotCompilationSample.TypeFirst` |
| DataLoader, default access | DataLoader usage through `IDataLoaderContextAccessor` | `samples/GraphQL.DataLoader.Sample.Default` |
| DataLoader, DI access | DataLoader usage with dependency injection | `samples/GraphQL.DataLoader.Sample.DI` |
| Federation, code-first | Apollo Federation with a code-first schema | `samples/GraphQL.Federation.CodeFirst.Sample3` |
| Federation, schema-first | Apollo Federation with schema-first setup | `samples/GraphQL.Federation.SchemaFirst.Sample1` and `samples/GraphQL.Federation.SchemaFirst.Sample2` |
| Federation, type-first | Apollo Federation with a type-first schema | `samples/GraphQL.Federation.TypeFirst.Sample4` |
| Harness | ASP.NET Core host that exercises multiple GraphQL.NET features | `samples/GraphQL.Harness` |
| StarWars, code-first | Introductory code-first schema | `src/GraphQL.StarWars` |
| StarWars, type-first | Introductory type-first schema | `src/GraphQL.StarWars.TypeFirst` |

## Running Samples

Run a sample project from the repository root:

```bash
dotnet run --project samples/GraphQL.CustomValidationRule.Sample/GraphQL.CustomValidationRule.Sample.csproj
```

Replace the project path with any sample listed above.

## Server Samples

The main repository focuses on the core GraphQL library. For ASP.NET Core server integration samples, see the [GraphQL.NET Server samples](https://github.com/graphql-dotnet/server/tree/master/samples).
