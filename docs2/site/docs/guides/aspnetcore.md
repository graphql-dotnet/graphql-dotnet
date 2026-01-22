# ASP.NET Core Integration

The [GraphQL.Server](https://github.com/graphql-dotnet/server) project provides middleware and supporting infrastructure for hosting GraphQL endpoints in ASP.NET Core applications. It includes support for GraphQL queries, mutations, subscriptions, and popular GraphQL IDEs like GraphiQL and Playground.

For complete documentation, samples, and advanced configuration options, please refer to the [server repository README](https://github.com/graphql-dotnet/server).

## Quick Start

Below is a complete sample of a .NET 8 console app that hosts a GraphQL endpoint at `http://localhost:5000/graphql`:

### Project file

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="GraphQL.Server.All" Version="8.3.3" />
  </ItemGroup>

</Project>
```

### Program.cs file

```csharp
using GraphQL;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()  // schema
    .AddSystemTextJson());   // serializer

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseGraphQL("/graphql");            // url to host GraphQL endpoint
app.UseGraphQLGraphiQL(
    "/",                               // url to host GraphiQL at
    new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
    {
        GraphQLEndPoint = "/graphql",         // url of GraphQL endpoint
        SubscriptionsEndPoint = "/graphql",   // url of GraphQL endpoint
    });
await app.RunAsync();
```

### Schema

```csharp
public class Query
{
    public static string Hero() => "Luke Skywalker";
}
```

### Sample request url

```
http://localhost:5000/graphql?query={hero}
```

### Sample response

```json
{"data":{"hero":"Luke Skywalker"}}
```

## Additional Resources

The [GraphQL.Server repository](https://github.com/graphql-dotnet/server) contains extensive documentation covering:

* Configuration options
* WebSocket support for subscriptions
* Authorization and authentication
* Multiple UI options (GraphiQL, Playground, Altair, Voyager)
* Sample projects and templates
* Performance optimization
* Error handling
* And much more

Please visit the [server repository](https://github.com/graphql-dotnet/server) for detailed information and examples.
