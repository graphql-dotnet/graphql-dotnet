# Samples

The repository includes sample projects for common GraphQL.NET setup styles. Use them when you want to compare complete projects rather than isolated documentation snippets.

| Sample | Approach | Use when |
| --- | --- | --- |
| [`GraphQL.SchemaFirst.Sample`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.SchemaFirst.Sample) | Schema first | You want to start with GraphQL SDL and map fields to .NET resolver classes. |
| [`GraphQL.Harness`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.Harness) | GraphType first with ASP.NET Core | You want a web-hosted sample with common GraphQL UI middleware. |
| [`GraphQL.StarWars`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.StarWars) | GraphType first | You want a reusable in-memory schema used by tests and the harness sample. |
| [`GraphQL.StarWars.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.StarWars.TypeFirst) | Type first | You want GraphQL.NET to infer schema metadata from CLR types and attributes. |
| [`GraphQL.AotCompilationSample.CodeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.CodeFirst) | GraphType first with AOT | You want to see the extra registrations needed for native AOT. |
| [`GraphQL.AotCompilationSample.TypeFirst`](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples/GraphQL.AotCompilationSample.TypeFirst) | Type first with AOT | You want type-first schema generation in a trimmed/AOT application. |
| Federation samples | Federation | You are building Apollo Federation-compatible schemas. |

## Schema-first sample

`GraphQL.SchemaFirst.Sample` demonstrates the schema-first flow:

1. Define the schema using GraphQL SDL.
2. Build the schema with `Schema.For`.
3. Register resolver classes with `builder.Types.Include<T>()`.
4. Execute a query against the schema.

The sample keeps the model small so the schema-to-resolver mapping is easy to inspect:

```csharp
using var schema = Schema.For(schemaDefinition, builder =>
{
    builder.Types.Include<Query>();
    builder.Types.Include<Droid>();
});
```

Use `[GraphQLMetadata]` when the CLR method or type name should map to a specific GraphQL name. For example, the sample maps `GetHero` to the `hero` field and `GetDroid` to the `droid` field.

Schema-first is a good fit when your team wants the GraphQL schema language to be the source of truth. It is less flexible than GraphType-first configuration for advanced runtime customization, so switch to GraphType-first when you need direct access to all `GraphType`, field, or schema configuration APIs.

## Schema-first support notes

Schema-first projects can:

- Define object, input object, enum, interface, union, query, mutation, and subscription types in SDL.
- Resolve fields from CLR properties and methods on registered resolver classes.
- Use `[GraphQLMetadata]` to map CLR names to GraphQL names or add metadata.
- Register additional types with `builder.Types.Include<T>()`.
- Use custom scalars and type mappings when they are registered on the schema.

Schema-first projects are not the best fit when:

- You need to configure many fields through `GraphType` APIs.
- You want compile-time discovery from CLR types instead of SDL as the source of truth.
- You are targeting native AOT or aggressive trimming and cannot root the resolver and model types used by reflection.
- You need extensive per-field customization that is clearer in `ObjectGraphType` classes.
