# Installation

For the core library and execution engine:
```
dotnet add package GraphQL
```

For a serializer (`IGraphQLSerializer` implementation):
```
dotnet add package GraphQL.SystemTextJson // recommended for .NET Core 3+
dotnet add package GraphQL.NewtonsoftJson
// or bring your own
```
