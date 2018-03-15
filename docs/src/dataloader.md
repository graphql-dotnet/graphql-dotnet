<!--Title:DataLoader-->
<!--Url:dataloader-->

GraphQL .NET includes an implementation of Facebook's [DataLoader](https://github.com/facebook/dataloader). 

Consider a GraphQL query like this:

```
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

3. Add the `DataLoaderDocumentListener` to the `DocumentExecuter`.

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

Then use the the `Context` property on the accessor to get the current `DataLoaderContext`. Each request will have its own context instance.

Use one of the "GetOrAddLoader" methods on the `DataLoaderContext`. These methods all require a string key to uniquely identify each loader. They also require a delegate for fetching the data. Each method will get an existing loader or add a new one, identified by the string key. Each method has various overloads to support different ways to load and map data with the keys.

Call `LoadAsync()` on the data loader. This will queue the request and return a `Task<T>`. If the result has already been cached, the task returned will already be completed. 

The `DataLoaderDocumentListener` configured in the set up steps above automatically handles dispatching all pending data loader operations at each step of the document execution. 

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
                // The task will complete once the GetUsersByIdAsync() returns with the batched results
                return loader.LoadAsync(context.Source.UserId);
            });
    }
}

public interface IUsersStore
{
	// This will be called by the loader for all pending keys
	// Note that fetch delegates can accept a CancellationToken parameter or not
	Task<Dictionary<int, User>> GetUsersByIdAsync(IEnumerable<int> userIds, CancellationToken cancellationToken);
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
                // The task will complete with an IEnumberable<Order> once the fetch delegate has returned
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
