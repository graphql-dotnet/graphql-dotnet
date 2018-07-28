# Introduction

[GraphQL.org](http://graphql.org/learn) is the best place to get started learning GraphQL.  Here is an excerpt from the introduction:

> GraphQL is a query language for your API, and a server-side runtime for executing queries by using a type system you define for your data. GraphQL isn't tied to any specific database or storage engine and is instead backed by your existing code and data.

> A GraphQL service is created by defining types and fields on those types, then providing functions for each field on each type.

Here is a "Hello World" for GraphQL .NET.

```csharp
using System;
using GraphQL;
using GraphQL.Types;

public class Program
{
  public static void Main(string[] args)
  {
    var schema = Schema.For(@"
      type Query {
        hello: String
      }
    ");

    var json = schema.Execute(_ =>
    {
      _.Query = "{ hello }";
      _.Root = new { Hello = "Hello World!" };
    });

    Console.WriteLine(json);
  }
}
```

Output
```json
{
  "data": {
    "hello": "Hello World!"
  }
}
```

There are two ways you can build your schema.  One is with a Handler / Enpoints approach using Schema Syntax.  The other is through GraphTypes.  The basics of both are demonstrated using the following schema definition.

> The `!` signifies a field is non-nullable.

```graphql
type StarWarsQuery {
  hero: Droid
}

type Droid {
  id: String!
  name: String!
}
```

## Handler / Endpoints Approach

The endpoints approach relys upon Schema Syntax, coding conventions, and tries to provide a minmal amount of syntax.  It is the easiest to get started using though does not currently support some advanced scenarios.

> Use the `GraphQLMetadata` attribute to customize the endpoint

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  [GraphQLMetadata("hero")]
  public Droid GetHero()
  {
    return new Droid { Id = "1", Name = "R2-D2" };
  }
}

var schema = Schema.For(@"
  type Droid {
    id: String!
    name: String!
  }

  type Query {
    hero: Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = schema.Execute(_ =>
{
  _.Query = "{ hero { id name } }";
});
```

Output
```json
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2"
    }
  }
}
```

## GraphType Types Approach

The `GraphType` "Type" approach can be more verbose, but gives you access to all of the provided properties of your `GraphType`'s and `Schema`.  You are required to use inheritance to leverage that functionality.

```csharp
using System;
using GraphQL;
using GraphQL.Types;

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

public class Program
{
  public static void Main(string[] args)
  {
    var schema = new Schema { Query = new StarWarsQuery() };

    var json = schema.Execute(_ =>
    {
      _.Schema = schema;
      _.Query = "hero { id name }";
    });

    Console.WriteLine(json);
  }
}
```

Output
```json
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2"
    }
  }
}
```
