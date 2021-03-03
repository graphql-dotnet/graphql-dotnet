## Installation

You can install the latest stable version via [NuGet](https://www.nuget.org/packages/GraphQL.Federation/).
```
> dotnet add package GraphQL.Federation
```

## Configuration

Register `FederatedInstrumentFieldMiddleware` in the DI
```
services.AddSingleton<FederatedInstrumentFieldMiddleware>();
```

```
var result = await _executer.ExecuteAsync(options =>
{
    // check if the tracing is enabled through header
    if (context.Request.IsFederatedTracingEnabled())
    {
        // enabled metrics collection
        options.EnableMetrics = _settings.EnableMetrics
        // use the middle to process field metrics
        options.FieldMiddleware.Use<FederatedInstrumentFieldMiddleware>();
    }
});

// add the tracing data to the result
if (context.Request.IsFederatedTracingEnabled())
{
    result.EnrichWithFederatedTracing(start);
}
```

### Testing

Include `apollo-federation-include-trace : ftv1` header with the request and the
tracing data will be returned in `extensions["ftv1"]` of the response.

