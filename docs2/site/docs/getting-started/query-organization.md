# Query Organization

In GraphQL there is only a single root `Query` object. This can make your root objects bloat
with unrelated functionality. You can group sets of functionality by adding a top level group.
You can apply this same trick to mutations and subscriptions.

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
    Field<CustomerGraphType>("customer").Arguments(...).Resolve(...);
    Field<OrderGraphType>("order").Arguments(...).Resolve(...);
    Field<ListGraphType<ProductGraphType>>("products").Arguments(...).Resolve(...);
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
    Field<AccountGroupGraphType>("account").Resolve(context => new {});
    Field<RetailGroupGraphType>("retail").Resolve(context => new {});
  }
}

public class AccountGroupGraphType : ObjectGraphType
{
  public AccountGroupGraphType()
  {
    Name = "Account";
    Field<CustomerGraphType>("customer").Arguments(...).Resolve(...);
    Field<OrderGraphType>("order").Arguments(...).Resolve(...);
  }
}

public class RetailGroupGraphType : ObjectGraphType
{
  public RetailGroupGraphType()
  {
    Name = "Retail";
    Field<ListGraphType<ProductGraphType>>("products").Arguments(...).Resolve(...);
  }
}
```

This allows you to separate out your queries into separate source files to keep your code
base cleaner. However, it will mean that your queries are 'nested' a layer deeper than
before, and you will need to take this into account when querying. For example, the above
'Retail' example, which could be queried in the playground with:

```graphql
{
  products {
    name
  }
}
```

Will now require

```graphql
{
  retail {
    products {
      name
    }
  }
}
```
