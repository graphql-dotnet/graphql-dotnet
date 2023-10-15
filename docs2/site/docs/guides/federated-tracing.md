## Installation

You can install the latest stable version via [NuGet](https://www.nuget.org/packages/GraphQL.Federation).

```
> dotnet add package GraphQL.Federation
```

## Configuration

Via `IGraphQLBuilder`:

```csharp
using GraphQL.Utilities.Federation; // from GraphQL.Server.Transports.AspNetCore nuget package

services.AddGraphQL(b => b
    // other registrations
    .UseApolloFederatedTracing(opts => {
        // pull the HttpContext
        var context = opts.RequestServices!.GetRequiredService<IHttpContextAccessor>().HttpContext;
        // check if the request has the header requesting federated tracing
        return context?.Request.IsApolloFederatedTracingEnabled() ?? false;
    }));
```

Otherwise, register `InstrumentFieldsMiddleware` in the DI:

```csharp
services.AddSingleton<InstrumentFieldsMiddleware>();
```

Add it to the schema:

```csharp
// pull instrumentFieldsMiddleware from DI, then:
schema.FieldMiddleware.Use(instrumentFieldsMiddleware);
```

Configure document executor and collect trace
```
var result = await _executer.ExecuteAsync(options =>
{
    // enable metrics collection
    options.EnableMetrics = _settings.EnableMetrics;
});

// add the tracing data to the result
result.EnrichWithApolloFederatedTracing(start);
```

Use [IsApolloFederatedTracingEnabled](https://github.com/graphql-dotnet/server/blob/master/src/Transports.AspNetCore/Extensions/GraphQLHttpRequestExtensions.cs#L15)
extension method to check if federated tracing is enabled/requested before calling
`EnrichWithApolloFederatedTracing`.

### Testing

Include `apollo-federation-include-trace: ftv1` header with the request and the
tracing data will be returned in `extensions["ftv1"]` of the response.
