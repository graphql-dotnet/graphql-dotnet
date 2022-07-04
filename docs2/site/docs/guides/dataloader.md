# DataLoader

GraphQL.NET includes an implementation of Facebook's [DataLoader](https://github.com/facebook/dataloader) within the
[`GraphQL.DataLoader`](https://www.nuget.org/packages/GraphQL.DataLoader) NuGet package.

Consider a GraphQL query like this:

```graphql
{
	orders(date: "2017-01-01") {
		orderId
		date
		user {
			userId
			firstName
			lastName
		}
	}
}
```

When the query is executed, first a list of orders is fetched. Then for each order, the associated user must also be fetched. If each user is fetched one-by-one, this would get more inefficient as the number of orders (N) grows. This is known as the N+1 problem. If there are 50 orders (N = 50), 51 separate requests would be made to load this data.

A DataLoader helps in two ways:

1. Similar operations are batched together. This can make fetching data over a network much more efficient.
2. Fetched values are cached so if they are requested again, the cached value is returned.

In the example above, a using a DataLoader will allow us to batch together all of the requests for the users. So there would be 1 request to retrieve the list of orders and 1 request to load all users associated with those orders. This would always be a total of 2 requests rather than N+1.

## Setup

1. Register `IDataLoaderContextAccessor` in your IoC container.
2. Register `DataLoaderDocumentListener` in your IoC container.

``` csharp
services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
services.AddSingleton<DataLoaderDocumentListener>();
```

3. Hook up your GraphQL schema to your IoC container.

``` csharp
public class MySchema : Schema
{
    public MySchema(IServiceProvider services) : base(services)
    {
    }
}
```

``` csharp
services.AddSingleton<MySchema>();
```

4. Add the `DataLoaderDocumentListener` to the `DocumentExecuter`.

``` csharp
var listener = Services.GetRequiredService<DataLoaderDocumentListener>();

var executer = new DocumentExecuter();
var result = executer.ExecuteAsync(opts => {

	...

	opts.Listeners.Add(listener);
});
```

## Usage

First, inject the `IDataLoaderContextAccessor` into your GraphQL type class.

Then use the `Context` property on the accessor to get the current `DataLoaderContext`. The `DataLoaderDocumentListener` configured above ensures that each request will have its own context instance.

Use one of the "GetOrAddLoader" methods on the `DataLoaderContext`. These methods all require a string key to uniquely identify each loader. They also require a delegate for fetching the data. Each method will get an existing loader or add a new one, identified by the string key. Each method has various overloads to support different ways to load and map data with the keys.

Call `LoadAsync()` on the data loader. This will queue the request and return a `IDataLoaderResult<T>`. If the result has already been cached, the returned value will be pulled from the cache.

The `ExecutionStrategy` will dispatch queued data loaders after all other pending fields have been resolved.

If your code requires an asynchronous call prior to queuing the data loader, use the `ResolveAsync` field builder method to return a
`Task<IDataLoaderResult<T>>`. The execution strategy will start executing the asynchronous code as soon as the field resolver executes.
Once the `IDataLoaderResult<T>` is retrieved from the asynchronous task, the data loader will be queued to be dispatched once all
other pending fields have been resolved.

To execute code within the resolver after the data loader has retrieved the data, pass a delegate to the `Then` extension
method of the returned `IDataLoaderResult<T>`. You can use a synchronous or asynchronous delegate, and it can return another
`IDataLoaderResult<T>` if you wish to chain dataloaders together. This may result in the field builder's Resolve delegate
signature looking like `IDataLoaderResult<IDataLoaderResult<T>>`, which is correct and will be handled properly by the execution strategy.

## Examples

This is an example of using a DataLoader to batch requests for loading items by a key. `LoadAsync()` is called by the field resolver for each Order. `IUsersStore.GetUsersByIdAsync()` will be called with the batch of userIds that were requested.

``` csharp
public class OrderType : ObjectGraphType<Order>
{
    // Inject the IDataLoaderContextAccessor to access the current DataLoaderContext
    public OrderType(IDataLoaderContextAccessor accessor, IUsersStore users)
    {
        ...

        Field<UserType, User>()
            .Name("User")
            .ResolveAsync(context =>
            {
                // Get or add a batch loader with the key "GetUsersById"
                // The loader will call GetUsersByIdAsync for each batch of keys
                var loader = accessor.Context.GetOrAddBatchLoader<int, User>("GetUsersById", users.GetUsersByIdAsync);

                // Add this UserId to the pending keys to fetch
                // The execution strategy will trigger the data loader to fetch the data via GetUsersByIdAsync() at the
                //   appropriate time, and the field will be resolved with an instance of User once GetUsersByIdAsync()
                //   returns with the batched results
                return loader.LoadAsync(context.Source.UserId);
            });
    }
}

public interface IUsersStore
{
    // This will be called by the loader for all pending keys
    // Note that fetch delegates can accept a CancellationToken parameter or not
    Task<IDictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
}
```

This is an example of using a DataLoader to batch requests for loading a collection of items by a key. This is used when a key may be associated with more than one item. `LoadAsync()` is called by the field resolver for each User. A User can have zero to many Orders. `IOrdersStore.GetOrdersByUserIdAsync` will be called with a batch of userIds that have been requested.

``` csharp
public class UserType : ObjectGraphType<User>
{
    // Inject the IDataLoaderContextAccessor to access the current DataLoaderContext
    public UserType(IDataLoaderContextAccessor accessor, IOrdersStore orders)
    {
        ...

        Field<ListGraphType<OrderType>, IEnumerable<Order>>()
            .Name("Orders")
            .ResolveAsync(ctx =>
            {
                // Get or add a collection batch loader with the key "GetOrdersByUserId"
                // The loader will call GetOrdersByUserIdAsync with a batch of keys
                var ordersLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Order>("GetOrdersByUserId",
                    orders.GetOrdersByUserIdAsync);

                // Add this UserId to the pending keys to fetch data for
                // The execution strategy will trigger the data loader to fetch the data via GetOrdersByUserId() at the
                //   appropriate time, and the field will be resolved with an instance of IEnumerable<Order> once
                //   GetOrdersByUserId() returns with the batched results
                return ordersLoader.LoadAsync(ctx.Source.UserId);
            });
    }
}

public class OrdersStore : IOrdersStore
{
	public async Task<ILookup<int, Order>> GetOrdersByUserIdAsync(IEnumerable<int> userIds)
	{
		var orders = await ... // load data from database

		return orders
			.ToLookup(x => x.UserId);
	}
}

```

This is an example of using a DataLoader without batching. This could be useful if the data may be requested multiple times. The result will be cached the first time. Subsequent calls to `LoadAsync()` will return the cached result.

``` csharp
public class QueryType : ObjectGraphType
{
    // Inject the IDataLoaderContextAccessor to access the current DataLoaderContext
    public QueryType(IDataLoaderContextAccessor accessor, IUsersStore users)
    {
        Field<ListGraphType<UserType>, IEnumerable<User>>()
            .Name("Users")
            .Description("Get all Users")
            .ResolveAsync(ctx =>
            {
                // Get or add a loader with the key "GetAllUsers"
                var loader = accessor.Context.GetOrAddLoader("GetAllUsers",
                    () => users.GetAllUsersAsync());

                // Prepare the load operation
                // If the result is cached, a completed Task<IEnumerable<User>> will be returned
                return loader.LoadAsync();
            });
    }
}

public interface IUsersStore
{
	Task<IEnumerable<User>> GetAllUsersAsync();
}
```

This is an example of using two chained DataLoaders to batch requests together, with asynchronous code before the data loaders execute, and post-processing afterwards.

``` csharp
public class UserType : ObjectGraphType<User>
{
    // Inject the IDataLoaderContextAccessor to access the current DataLoaderContext
    public UserType(IDataLoaderContextAccessor accessor, IUsersStore users, IOrdersStore orders, IItemsStore items)
    {
        ...

        Field<ListGraphType<ItemType>, IEnumerable<Item>>()
            .Name("OrderedItems")
            .ResolveAsync(async context =>
            {
                // Asynchronously authenticate
                var valid = await users.CanViewOrders(context.Source.UserId);
                if (!valid) return null;
                
                // Get or add a collection batch loader with the key "GetOrdersByUserId"
                // The loader will call GetOrdersByUserIdAsync with a batch of keys
                var ordersLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Order>("GetOrdersByUserId",
                    orders.GetOrdersByUserIdAsync);

                var ordersResult = ordersLoader.LoadAsync(context.Source.UserId);

                // Once the orders have been retrieved by the first data loader, feed the order IDs into
                //   the second data loader
                return ordersResult.Then((orders, cancellationToken) =>
                {
                    // Collect all of the order IDs
                    var orderIds = orders.Select(o => o.Id);

                    // Get or add a collection batch loader with the key "GetItemsByOrderId"
                    // The loader will call GetItemsByOrderId with a batch of keys
                    var itemsLoader = accessor.Context.GetOrAddCollectionBatchLoader<int, Item>("GetItemsByOrderId",
                        items.GetItemsByOrderIdAsync);

                    var itemsResults = itemsLoader.LoadAsync(orderIds);

                    // itemsResults is of type IDataLoaderResult<IEnumerable<Item>[]> so the array needs to be flattened
                    //   before returning it back to the query
                    return itemsResults.Then(itemResultSet =>
                    {
                        // Flatten the results after the second dataloader has finished
                        return flattenedResults = itemResultSet.SelectMany(x => x);
                    });
                });
            });
    }
}

public interface IUsersStore
{
    // This will be called for each call to OrderedItems, prior to any data loader execution
    Task<bool> CanViewOrders(int userId);
}
public interface IOrdersStore
{
    // This will be called by the "order" loader for all pending keys
    // Note that fetch delegates can accept a CancellationToken parameter or not
    Task<ILookup<int, Order>> GetOrdersByUserIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
}
public interface IItemsStore
{
    // This will be called by the "item" loader for all pending keys
    // Note that fetch delegates can accept a CancellationToken parameter or not
    Task<ILookup<int, Item>> GetItemsByOrderIdAsync(IEnumerable<int> orderIds, CancellationToken cancellationToken);
}
```
> See this [blog series](https://fiyazhasan.me/graphql-with-net-core-part-xi-dataloader/) for an in depth example using Entity Framework.

## Exceptions

Exceptions within data loaders' fetch delegates are passed back to the execution strategy for all associated fields.
If you have a need to capture exceptions raised by the fetch delegate, create a `new SimpleDataLoader<T>` within
your field resolver (do not use the `IDataLoaderContextAccessor` for this) and have its fetch delegate await the
`IDataLoaderResult<T>.GetResultAsync` method of the result obtained from the first data loader within a try/catch
block. Return the result of the simple data loader's `LoadAsync()` function to the field resolver.  The data loader
will still load at the appropriate time, and you can handle exceptions as desired.

## DI-based data loaders

The above instructions describe how to use the data loader context and accessor classes to create data loaders scoped
to the current request. You can also use dependency injection to register a data loader instance. This can eliminate
duplicated code if you call the same data loader from different field resolvers. It can also help to prevent
unforseen bugs due to a data loader fetch delegate capturing variables from a field resolver's scope.

To create a custom and register a custom data loader instance, first create a class and inherit `DataLoaderBase<TKey, T>`.
Override the `FetchAsync` method with the code to retrieve the data based on the provided keys. Call `SetResult` on
each provided `DataLoaderPair` to set the result. Feel free to use dependency injection to rely on any scoped services
necessary to facilitate execution of the fetch method. See below sample:

```csharp
public class Order
{
    public int Id { get; set; }
    public string ShipToName { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ItemName { get; set; }
}

// similar to BatchDataLoader
public class MyOrderDataLoader : DataLoaderBase<int, Order>
{
    private readonly MyDbContext _dbContext;
    public MyOrderDataLoader(MyDbContext dataContext)
    {
        _dbContext = dataContext;
    }

    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, Order>> list, CancellationToken cancellationToken)
    {
        IEnumerable<int> ids = list.Select(pair => pair.Key);
        IDictionary<int, Order> data = await _dbContext.Orders.Where(order => ids.Contains(order.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        foreach (DataLoaderPair<int, Order> entry in list)
        {
            entry.SetResult(data.TryGetValue(entry.Key, out var order) ? order : null);
        }
    }
}

// similar to CollectionBatchDataLoader
public class MyOrderItemsDataLoader : DataLoaderBase<int, IEnumerable<OrderItem>>
{
    private readonly MyDbContext _dbContext;
    public MyOrderItemsDataLoader(MyDbContext dataContext)
    {
        _dbContext = dataContext;
    }

    protected override Task FetchAsync(IEnumerable<DataLoaderPair<int, IEnumerable<OrderItem>>> list, CancellationToken cancellationToken)
    {
        IEnumerable<int> ids = list.Select(pair => pair.Key);
        IEnumerable<OrderItem> data = await _dbContext.OrderItems.Where(orderItem => ids.Contains(orderItem.OrderId)).ToListAsync(cancellationToken);
        ILookup<int, OrderItem> dataLookup = data.ToLookup(x => x.OrderId);
        foreach (DataLoaderPair<int, IEnumerable<OrderItem>> entry in list)
        {
            entry.SetResult(dataLookup[entry.Key]);
        }
    }
}
```

You will need to register the data loader as a scoped service within your DI framework.

```csharp
services.AddScoped<MyOrderDataLoader>();
services.AddScoped<MyOrderItemsDataLoader>();
```

Then within your field resolvers, access the data loader via the `RequestServices` property and call `LoadAsync` as before:

```csharp
public class MyQuery : ObjectGraphType
{
    public MyQuery()
    {
        Field<OrderType, Order>()
            .Name("Order")
            .Argument<IdGraphType>("id")
            .ResolveAsync(context =>
            {
                // Get the custom data loader
                var loader = context.RequestServices.GetRequiredService<MyOrderDataLoader>();

                // Add this UserId to the pending keys to fetch.
                // The execution strategy will trigger the data loader to fetch the data via MyOrderDataLoader.FetchAsync() at the
                // appropriate time, and the field will be resolved with an instance of Order once FetchAsync()
                // returns with the batched results
                return loader.LoadAsync(context.GetArgument<int>("id"));
            });
    }
}

public class OrderType : ObjectGraphType<Order>
{
    public OrderType()
    {
        Field(x => x.Id, type: typeof(IdGraphType));
        Field(x => x.ShipToName);
        Field<ListGraphType<OrderItemType>, IEnumerable<OrderItem>>()
            .Name("Items")
            .ResolveAsync(context =>
            {
                var loader = context.RequestServices.GetRequiredService<MyOrderItemsDataLoader>();
                return loader.LoadAsync(context.Source.Id);
            });
    }
}
```

You do not need to use `IDataLoaderContextAccessor` or `DataLoaderDocumentListener` and may remove those references
from your code.

## Singleton DI-based data loader instances

If you wish to register the data loader as a singleton, be sure to disable caching by calling `: base(false)` in
the constructor, as the cache entries never expire. You will also need to be sure your code does not rely on any
scoped services, or create a dedicated service scope within the fetch method as shown below.

```csharp
public class MyOrderDataLoader : DataLoaderBase<int, Order>
{
    private readonly IServiceProvider _rootServiceProvider;
    public MyOrderDataLoader(IServiceProvider serviceProvider) : base(false)
    {
        _rootServiceProvider = serviceProvider;
    }

    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, Order>> list, CancellationToken cancellationToken)
    {
        using (var scope = _rootServiceProvider.CreateScope())
        {
            MyDbContext dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            IEnumerable<int> ids = list.Select(pair => pair.Key);
            IDictionary<int, Order> data = await dbContext.Orders.Where(order => ids.Contains(order.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (DataLoaderPair<int, Order> entry in list)
            {
                entry.SetResult(data.TryGetValue(entry.Key, out var order) ? order : null);
            }
        }
    }
}
```

As a singleton, you can pull the singleton instance into your graphtype class in its constructor.

```csharp
    public class MyQuery : ObjectGraphType
    {
        public MyQuery(MyOrderDataLoader loader)
        {
            Field<OrderType, Order>()
                .Name("Order")
                .Argument<IdGraphType>("id")
                .ResolveAsync(context =>
                {
                    return loader.LoadAsync(context.GetArgument<int>("id"));
                });
        }
    }
```

## Adding a global cache

Data loaders will, by default, cache values returned for a given key for the lifetime of a request. You can change
the fetch method of your data loader to use a global cache. The below sample demonstrates changes required to a singleton
DI-based data loader as shown immediately above, using the `Microsoft.Extensions.Caching.Memory` NuGet package.

```csharp
public class MyOrderDataLoader : DataLoaderBase<int, Order>
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;
    private const string CACHE_PREFIX = "ORDER_";
        
    public MyOrderDataLoader(IServiceProvider serviceProvider, IMemoryCache memoryCache) : base(false)
    {
        _rootServiceProvider = serviceProvider;
        _memoryCache = memoryCache;
        _memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            // specify a maximum lifetime of 5 minutes
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            // set so that the size of the cache can be limited
            Size = 1,
        };
    }

    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<int, Order>> list, CancellationToken cancellationToken)
    {
        // create a list of keys that are not in the cache
        var unMatched = new List<DataLoaderPair<int, Order>>(list.Count());
        // attempt to match any keys possible from the global cache
        foreach (var entry in list)
        {
            if (_memoryCache.TryGetValue(CACHE_PREFIX + entry.Key, out var value))
            {
                entry.SetResult((Order)value);
            }
            else
            {
                unMatched.Add(entry);
            }
        }
        // process the unmatched keys as usual
        list = unMatched;
        using (var scope = _rootServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            IEnumerable<int> ids = list.Select(pair => pair.Key);
            IDictionary<int, Order> data = await dbContext.Orders.Where(order => ids.Contains(order.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
            foreach (DataLoaderPair<int, Order> entry in list)
            {
                if (data.TryGetValue(entry.Key, out var order))
                {
                    // only save the entry in the cache if it was found in the database
                    _memoryCache.Set(CACHE_PREFIX + entry.Key, order, _memoryCacheEntryOptions);
                    entry.SetResult(order);
                }
                else
                {
                    entry.SetResult(null);
                }
            }
        }
    }
}

// also, register the memory cache in your DI configuration
// limit cache to 10,000 entries
services.AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions { SizeLimit = 10000 }));
```
