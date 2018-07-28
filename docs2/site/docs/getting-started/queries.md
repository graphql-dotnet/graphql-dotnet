# Queries

To perform a query you need to have a root Query object that is an `ObjectGraphType`.  Queries should only fetch data and never modify it.  You can only have a single root Query object.

```graphql
query {
  hero {
    id
    name
  }
}
```

```csharp
var schema = new StarWarsSchema();
var json = schema.Execute(_ =>
{
  _.Query = @"
    query {
      hero {
        id
        name
      }
    }
  ";
});
```

```csharp
public class StarWarsSchema : Schema
{
  public StarWarsSchema()
  {
    Query = new StarWarsQuery();
  }
}

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
