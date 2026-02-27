# DataLoader

GraphQL .NET includes a DataLoader implementation inspired by the [Facebook DataLoader](https://github.com/graphql/dataloader) for batching and caching database and service calls.

## Setup

1. Register `IDataLoaderContextAccessor` and `DataLoaderDocumentListener` with your IoC container.

```csharp
services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
services.AddSingleton<DataLoaderDocumentListener>();
```

2. Add the `DataLoaderDocumentListener` to the `DocumentExecuter`.

```csharp
var listener = _serviceProvider.GetRequiredService<DataLoaderDocumentListener>();

await _executer.ExecuteAsync(opts =>
{
    ...
    opts.Listeners.Add(listener);
});
```

### ASP.NET Core Setup

For ASP.NET Core projects, you can use the `AddDataLoader()` extension method in your `ConfigureServices` method as a convenient alternative to manually registering the services above:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ...

    services.AddDataLoader();

    // ...
}
```

This registers both `IDataLoaderContextAccessor` and `DataLoaderDocumentListener` with the DI container. You still need to add the `DataLoaderDocumentListener` as a listener when executing documents, as shown in step 2 above.

## Usage

Define a field that uses a DataLoader:

```csharp
Field<ListGraphType<OrderType>>(
    "orders",
    resolve: context =>
    {
        var loader = context.RequestServices
            .GetRequiredService<IDataLoaderContextAccessor>()
            .Context
            .GetOrAddBatchLoader<int, IEnumerable<Order>>(
                "GetOrdersByUserId",
                GetOrdersByUserIdAsync);

        return loader.LoadAsync(context.Source.Id);
    });
```
