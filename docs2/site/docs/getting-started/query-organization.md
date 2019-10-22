# Query Organization

In GraphQL there is only a single root `Query` object. This can make your root objects bloat with unrelated functionality.  You can group sets of functionality by adding a top level group.  You can apply this same trick to mutations and subscriptions.

```graphql
type Query {
  customer(id: ID): Customer
  order(id: ID): Order
  products: [Product]
}
```

```csharp
public class Query : ObjectGraphType
{
  public Query()
  {
    Name = "Query";
    Field<CustomerGraphType>("customer", arguments: ..., resolve:...);
    Field<OrderGraphType>("order", arguments: ..., resolve:...);
    Field<ListGraphType<ProductGraphType>>("products", arguments: ..., resolve:...);
  }
}
```

Split into groups.

```graphql
type Account {
  customer(id: ID): Customer
  order(id: ID): Order
}

type Retail {
  products: [Product]
}

type Query {
  account: Account
  retail: Retail
}
```

The trick is to return an empty object.

```csharp
public class Query : ObjectGraphType
{
  public Query()
  {
    Name = "Query";
    Field<AccountGroupGraphType>("account", resolve: context => new {});
    Field<RetailGroupGraphType>("retail", resolve: context => new {});
  }
}

public class AccountGroupGraphType : ObjectGraphType
{
  public AccountGroupGraphType()
  {
    Name = "Account";
    Field<CustomerGraphType>("customer", arguments: ..., resolve:...);
    Field<OrderGraphType>("order", arguments: ..., resolve:...);
  }
}

public class RetailGroupGraphType : ObjectGraphType
{
  public RetailGroupGraphType()
  {
    Name = "Retail";
    Field<ListGraphType<ProductGraphType>>("products", arguments: ..., resolve:...);
  }
}
```
