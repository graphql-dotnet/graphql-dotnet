# Introduction

[GraphQL.org](http://graphql.org/learn) is the best place to get started learning GraphQL.  Here is an excerpt from the introduction:

> GraphQL is a query language for your API, and a server-side runtime for executing queries by using a type system you define for your data. GraphQL isn't tied to any specific database or storage engine and is instead backed by your existing code and data.

> A GraphQL service is created by defining types and fields on those types, then providing functions for each field on each type.

Here is a "Hello World" for GraphQL .NET.

```graphql
type StarWarsQuery {
  hero: Droid
}

type Droid {
  id: String!
  name: String
}
```

```csharp
using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Types;

public class Program
{
  public static void Main(string[] args)
  {
    var schema = new Schema { Query = new StarWarsQuery() };

    var json = schema.Execute(_ =>
    {
      _.Schema = schema;
      _.Query = @"
          query {
            hero {
              id
              name
            }
          }
        ";
    });

    Console.WriteLine(json);
  }
}

public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class DroidType : ObjectGraphType<Droid>
{
  public DroidType()
  {
    Field(x => x.Id).Description("The Id of the Droid.");
    Field(x => x.Name, nullable: true).Description("The name of the Droid.");
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

Output
```
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2"
    }
  }
}
```
