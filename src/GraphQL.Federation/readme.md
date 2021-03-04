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
Configure document executor and collect trace
```
var result = await _executer.ExecuteAsync(options =>
{
    // enabled metrics collection
    options.EnableMetrics = _settings.EnableMetrics
});

// add the tracing data to the result
  result.EnrichWithFederatedTracing(start);
```

### Testing

Include `apollo-federation-include-trace : ftv1` header with the request and the
tracing data will be returned in `extensions["ftv1"]` of the response.

