# Server Project (ASP.NET Core)

GraphQL.NET provides the core execution engine. The `graphql-dotnet/server` project adds HTTP and WebSocket transport middleware for ASP.NET Core.

## Installation

Install either the all-in-one package:

```bash
dotnet add package GraphQL.Server.All
```

Or install transport + selected UI packages explicitly:

```bash
dotnet add package GraphQL.Server.Transports.AspNetCore
dotnet add package GraphQL.Server.Ui.GraphiQL
dotnet add package GraphQL.SystemTextJson
```

## Basic ASP.NET Core example

```csharp
using GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson());

var app = builder.Build();

app.UseWebSockets();
app.MapGraphQL("/graphql");
app.MapGraphQLGraphiQL("/ui/graphiql");

app.Run();

public class Query
{
    public static string Hero() => "Luke Skywalker";
}
```

After startup, you can test with:

- GraphQL endpoint: `/graphql`
- GraphiQL UI: `/ui/graphiql`

## Next steps

- For full server configuration options, see the server README:
  - https://github.com/graphql-dotnet/server
- For working reference projects, see server samples:
  - https://github.com/graphql-dotnet/server/tree/master/samples
