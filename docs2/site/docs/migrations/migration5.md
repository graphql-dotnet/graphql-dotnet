# Migrating from v4.x to v5.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A5.0+is%3Aissue+is%3Aclosed) and [pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A5.0+is%3Aclosed) done in v5.

## New Features

### `IGraphQLRequestReader` interface to support JSON deserialization

`IGraphQLRequestReader.ReadAsync` is implemented by the `GraphQL.SystemTextJson` and
`GraphQL.NewtonsoftJson` libraries. It supports deserialization of any type, with
special support for the `GraphQLRequest` class. It also supports deserializing to
a `IList<GraphQLRequest>`, which will deserialize multiple requests or
a single request (with or without the JSON array wrapper) into a list.

When calling the `AddSystemTextJson` or `AddNewtonsoftJson` extension method to
the `IGraphQLBuilder` interface, the method will register the `IDocumentWriter`
as usual, plus the `IGraphQLRequestReader` interfaces with the appropriate
serialization engine. JSON configuration options will normally apply to both
classes, but an additional overload is provided if you need to independently
configure the JSON serialization options for each class.

This makes it so that you can write JSON-based transport code independent of the
JSON serialization engine used by your application, simplifying the most common use
case, while still being configurable through your DI framework.

###

## Breaking Changes

###

###
