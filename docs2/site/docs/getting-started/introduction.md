# Introduction

[GraphQL.org](http://graphql.org/learn) is the best place to get started learning GraphQL.
Here is an excerpt from the introduction:

> GraphQL is a query language for your API, and a server-side runtime for executing queries
> by using a type system you define for your data. GraphQL isn't tied to any specific database
> or storage engine and is instead backed by your existing code and data.

> A GraphQL service is created by defining types and fields on those types, then providing
> functions for each field on each type.

Here is a "Hello World" example for GraphQL.NET using the System.Text.Json serialization engine.

```csharp
using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson;

public class Program
{
  public static async Task Main(string[] args)
  {
    var schema = Schema.For(@"
      type Query {
        hello: String
      }
    ");

    var json = await schema.ExecuteAsync(_ =>
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

There are two ways you can build your schema.  One is with a Schema first approach using
the [GraphQL schema language](https://graphql.org/learn/schema/#type-language). The other
is a `GraphType` or Code first approach by writing `GraphType` classes. The basics of
both are demonstrated using the following schema definition.

> `!` signifies a field is non-nullable.

```graphql
type Droid {
  id: String!
  name: String!
}

type Query {
  hero: Droid
}
```

## Schema First Approach

The Schema first approach relies upon the [GraphQL schema language](https://graphql.org/learn/schema/#type-language),
coding conventions, and tries to provide a minimal amount of syntax.  It is the easiest to get started though
it does not currently support some advanced scenarios.

> Use the optional `GraphQLMetadata` attribute to customize the mapping to the schema type.

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

var json = await schema.ExecuteAsync(_ =>
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

## GraphType First Approach

The `GraphType` first approach can be more verbose, but gives you access to all of the
provided properties of your `GraphType`'s and `Schema`. You are required to use
inheritance to leverage that functionality.

```csharp
using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson;

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
    Field(x => x.Name).Description("The name of the Droid.");
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
  public static async Task Main(string[] args)
  {
    var schema = new Schema { Query = new StarWarsQuery() };

    var json = await schema.ExecuteAsync(_ =>
    {
      _.Query = "{ hero { id name } }";
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

## Schema First Nested Types

You can provide multiple "top level" schema types using the Schema first approach.

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Character
{
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

[GraphQLMetadata("Droid", IsTypeOf=typeof(Droid))]
public class DroidType
{
  public string Id([FromSource] Droid droid) => droid.Id;
  public string Name([FromSource] Droid droid) => droid.Name;

  // these two parameters are optional
  // IResolveFieldContext provides contextual information about the field
  public Character Friend(IResolveFieldContext context, [FromSource] Droid source)
  {
    return new Character { Name = "C3-PO" };
  }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var schema = Schema.For(@"
          type Droid {
            id: String!
            name: String!
            friend: Character
          }

          type Character {
            name: String!
          }

          type Query {
            hero: Droid
          }
        ", _ =>
        {
            _.Types.Include<DroidType>();
            _.Types.Include<Query>();
        });

        var json = await schema.ExecuteAsync(_ =>
        {
            _.Query = "{ hero { id name friend { name } } }";
        });

        Console.WriteLine(json);
    }
}
```

Output:

```json
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2",
      "friend": {
        "name": "C3-PO"
      }
    }
  }
}
```
