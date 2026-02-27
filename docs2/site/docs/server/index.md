# GraphQL.NET Server

The [GraphQL.NET Server](https://github.com/graphql-dotnet/server) project provides middleware and tools
for hosting a GraphQL endpoint in an ASP.NET Core application. It supports:

- HTTP middleware for handling GraphQL queries over HTTP
- WebSocket support for GraphQL subscriptions
- GraphQL Playground and GraphiQL UIs
- Dependency injection integration

## Installation

Install the NuGet packages for the transport(s) you need:

```sh
# For HTTP transport (queries and mutations)
dotnet add package GraphQL.Server.Transports.AspNetCore

# For WebSocket transport (subscriptions)
dotnet add package GraphQL.Server.Transports.WebSockets

# For GraphQL Playground UI
dotnet add package GraphQL.Server.Ui.Playground

# For GraphiQL UI
dotnet add package GraphQL.Server.Ui.GraphiQL
```

## Basic Setup

### 1. Define your schema

```csharp
public class MySchema : Schema
{
    public MySchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<MyQuery>();
    }
}

public class MyQuery : ObjectGraphType
{
    public MyQuery()
    {
        Field<StringGraphType>("hello", resolve: context => "world");
    }
}
```

### 2. Register services

In your `Program.cs` (or `Startup.cs` for older projects), register GraphQL services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register your schema and query types
builder.Services.AddScoped<MyQuery>();
builder.Services.AddScoped<MySchema>();

// Add GraphQL server services
builder.Services
    .AddGraphQL(b => b
        .AddSchema<MySchema>()
        .AddSystemTextJson()   // or .AddNewtonsoftJson()
        .AddGraphTypes(typeof(MySchema).Assembly)
    );

var app = builder.Build();
```

### 3. Configure middleware

```csharp
// Map the GraphQL endpoint (HTTP POST)
app.UseGraphQL<MySchema>("/graphql");

// Optionally serve the GraphQL Playground UI
app.UseGraphQLPlayground("/ui/playground");

// Optionally serve GraphiQL
app.UseGraphQLGraphiQL("/ui/graphiql");

app.Run();
```

## Subscriptions

To enable GraphQL subscriptions over WebSockets, add the WebSocket middleware:

```csharp
// In services registration
builder.Services
    .AddGraphQL(b => b
        .AddSchema<MySchema>()
        .AddSystemTextJson()
        .AddWebSockets()  // enable WebSocket support
        .AddGraphTypes(typeof(MySchema).Assembly)
    );

// In middleware pipeline
app.UseWebSockets();
app.UseGraphQLWebSockets<MySchema>("/graphql");
app.UseGraphQL<MySchema>("/graphql");
```

For subscriptions to work, your schema must define a `Subscription` field and you need
a subscription type:

```csharp
public class MySchema : Schema
{
    public MySchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query        = serviceProvider.GetRequiredService<MyQuery>();
        Subscription = serviceProvider.GetRequiredService<MySubscription>();
    }
}

public class MySubscription : ObjectGraphType
{
    public MySubscription(IEventStore eventStore)
    {
        AddField(new EventStreamFieldType
        {
            Name = "messageAdded",
            Type = typeof(StringGraphType),
            Resolver = new FuncFieldResolver<string>(ctx => ctx.Source as string),
            Subscriber = new EventStreamResolver<string>(ctx => eventStore.Messages())
        });
    }
}
```

## Authentication and Authorization

The server project integrates with ASP.NET Core authentication and authorization.
You can restrict access to the GraphQL endpoint using standard ASP.NET Core middleware:

```csharp
app.UseAuthentication();
app.UseAuthorization();

// Require authenticated users for the GraphQL endpoint
app.UseGraphQL<MySchema>("/graphql", options =>
{
    options.AuthorizationRequired = true;
});
```

## Additional Resources

- [GraphQL.NET Server GitHub repository](https://github.com/graphql-dotnet/server)
- [Sample projects](https://github.com/graphql-dotnet/server/tree/develop/samples)
- [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/)
