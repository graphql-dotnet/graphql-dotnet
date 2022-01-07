# Queries

To perform a query you need to have a root Query object that is an `ObjectGraphType`.
Queries should only fetch data and never modify it.  You can only have a single root
Query object. By default queries are executed in parallel.

```graphql
query {
  hero {
    id
    name
  }
}
```

If you have only a single query, you can use shorthand syntax.

```graphql
hero {
  id
  name
}
```

To provide an `Operation` name for your query, you add it after the `query` keyword.
An `Operation` name is optional if there is only a single operation in the request.

```graphql
query MyHeroQuery {
  hero {
    id
    name
  }
}
```

You can also provide that operation name to the `ExecutionOptions`.

```csharp
var schema = new Schema { Query = new StarWarsQuery() };
var json = await schema.ExecuteAsync(_ =>
{
  _.OperationName = "MyHeroQuery";
  _.Query = @"
    query MyHeroQuery {
      hero {
        id
        name
      }
    }
  ";
});
```

```csharp
public class StarWarsQuery : ObjectGraphType
{
  public StarWarsQuery()
  {
    Field<DroidType>(
      "hero",
      resolve: context => new Droid { Id = "1", Name = "R2-D2" }
    );
  }
}
```
