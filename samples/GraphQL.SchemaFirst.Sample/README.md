# GraphQL.SchemaFirst.Sample

This sample demonstrates the schema-first approach with `Schema.For(...)` and SDL.

## What it demonstrates

- Defining schema types with SDL
- Mapping resolver methods via `GraphQLMetadata`
- Executing a query and printing JSON output

## Run

```bash
dotnet run --project samples/GraphQL.SchemaFirst.Sample/GraphQL.SchemaFirst.Sample.csproj
```

Expected output includes:

```json
{"data":{"hero":{"id":"1","name":"R2-D2"}}}
```
