# Basics

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
namespace ConsoleApplication
{
    using System;
    using System.Threading.Tasks;
    using GraphQL;
    using GraphQL.Http;
    using GraphQL.Types;

    public class Program
    {
        public static void Main(string[] args)
        {
          Run();
        }

        private static async void Run()
        {
          Console.WriteLine("Hello GraphQL!");

          var schema = new Schema { Query = new StarWarsQuery() };

          var result = await new DocumentExecuter().ExecuteAsync( _ =>
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
          }).ConfigureAwait(false);

          var json = new DocumentWriter(indent: true).Write(result);

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
}
```

Output
```
Hello GraphQL!
{
  "data": {
    "hero": {
      "id": "1",
      "name": "R2-D2"
    }
  }
}
```

# GraphiQL

[GraphiQL](https://github.com/graphql/graphiql) is an interactive in-browser GraphQL IDE.  This is a fantastic developer tool to help you form queries and explore your Schema.  The [sample project](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.GraphiQL) gives an example of hosting the GraphiQL IDE.

![](http://i.imgur.com/2uGdVAj.png)

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

public class StarWarsSchema : Schema
{
  public StarWarsSchema()
  {
    Query = new StarWarsQuery();
  }
}
```

# Arguments

You can provide arguments to a field.  You can use `GetArgument` on `ResolveFieldContext` to retrieve argument values.  `GetArgument` will attempt to coerce the argument values to the generic type it is given, including primitive values, objects, and enumerations.  You can gain access to the value directly through the `Arguments` dictionary on `ResolveFieldContext`.

```graphql
query {
  droid(id: "1") {
    id
    name
  }
}
```

```csharp
public class StarWarsQuery : ObjectGraphType
{
  public StarWarsQuery(IStarWarsData data)
  {
    Field<DroidType>(
      "droid",
      arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "id" }),
      resolve: context =>
      {
        var id = context.GetArgument<string>("id");
        var objectId = contet.Arguments["id"];
        return data.GetDroidByIdAsync(id);
      }
    );
  }
}
```

# Mutations

To perform a mutation you need to have a root Mutation object that is an `ObjectGraphType`.  Mutations make modifications to data and return a result.  You can only have a single root Mutation object.

```csharp
public class Mutation : ObjectGraphType
{
  public Mutation(IDocumentSession session)
  {
    Field<DinnerType>(
      "createDinner",
      arguments: new QueryArguments(new QueryArgument<DinnerInputType> { Name = "dinner" }),
      resolve: context =>
      {
        var userContext = context.UserContext.As<GraphQLUserContext>();
        var dinner = context.GetArgument<Dinner>("dinner");

        dinner.HostedById = userContext.User.UserName;
        dinner.HostedBy = string.IsNullOrWhiteSpace(dinner.HostedBy)
          ? userContext.User.FriendlyName
          : dinner.HostedBy;

        session.Store(dinner);
        session.SaveChanges();

        return dinner;
      });
  }
}

public class NerdDinnerSchema : Schema
{
  public NerdDinnerSchema()
  {
    Mutation = new Mutation();
  }
}
```

# Interfaces

A GraphQL Interface is an abstract type that includes a certain set of fields that a type must include to implement the interface.

Here is an interface that represents a `Character` in the StarWars universe.

```graphql
interface Character {
  id: ID!
  name: String!
  friends: [Character]
}
```

```csharp
public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
  public CharacterInterface()
  {
    Name = "Character";
    Field(d => d.Id).Description("The id of the character.");
    Field(d => d.Name, nullable: true).Description("The name of the character.");
    Field<ListGraphType<CharacterInterface>>("friends");
  }
}
```

Any type that implements `Character` needs to have these exact fields, arguments, and return types.

```graphql
type Droid implements Character {
  id: ID!
  name: String!
  friends: [Character]
  primaryFunction: String
}
```

```csharp
public class DroidType : ObjectGraphType<Droid>
{
  public DroidType(IStarWarsData data)
  {
    Name = "Droid";
    Description = "A mechanical creature in the Star Wars universe.";

    Field(d => d.Id).Description("The id of the droid.");
    Field(d => d.Name, nullable: true).Description("The name of the droid.");

    Field<ListGraphType<CharacterInterface>>(
      "friends",
      resolve: context => data.GetFriends(context.Source)
    );
    Field(d => d.PrimaryFunction, nullable: true).Description("The primary function of the droid.");

    Interface<CharacterInterface>();
  }
}
```

# IsTypeOf

`IsTypeOf` is a function which helps resolve the implementing GraphQL type during execution.  For example, when you have a field that returns a GraphQL Interface the engine needs to know which concrete Graph Type to use.  So if you have a `Character` interface that is implemented by both `Human` and `Droid` types, the engine needs to know which graph type to choose.  The data object being mapped is passed to the `IsTypeOf` function which should return a boolean value.

```csharp
public class DroidType : ObjectGraphType
{
  public DroidType(IStarWarsData data)
  {
    Name = "Droid";

    ...

    Interface<CharacterInterface>();

    IsTypeOf = obj => obj is Droid;
  }
}
```

> `ObjectGraphType<T>` provides a default implementation of IsTypeOf for you.

An alternate to using `IsTypeOf` is instead implementing `ResolveType` on the Interface or Union.  See the `ResolveType` section for more details.

# Unions

Unions are a composition of two different types.

```csharp
public class CatOrDog : UnionGraphType
{
  public CatOrDog()
  {
    Type<Cat>();
    Type<Dog>();
  }
}
```

# ResolveType

An alternate to using `IsTypeOf` is implementing `ResolveType` on the Interface or Union.  The major difference is `ResolveType` is required to be exhastive.  If you add another type that implements an Interface you are required to alter the Interface for that new type to be resolved.

> If a type implements `ResolveType` then any `IsTypeOf` implementation is ignored.

```csharp
public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
  public CharacterInterface(
    DroidType droidType,
    HumanType humanType)
  {
    Name = "Character";

    ...

    ResolveType = obj =>
    {
        if (obj is Droid)
        {
            return droidType;
        }

        if (obj is Human)
        {
            return humanType;
        }

        throw new ArgumentOutOfRangeException($"Could not resolve graph type for {obj.GetType().Name}");
    };
  }
}
```

# Variables

You can pass variables recieved from the client to the execution engine by using the `Inputs` property.

```csharp
var inputs = variablesJson.ToInputs();

var result = await executer.ExecuteAsync(_ =>
{
    _.Inputs = inputs;
});
```

# Query Validation

There [are a number of query validation rules](http://facebook.github.io/graphql/#sec-Validation) that are ran when a query is executed.  All of these are turned on by default.  You can add your own validation rules or clear out the existing ones by accessing the `ValidationRules` property.

```csharp
var result = await executer.ExecuteAsync(_ =>
{
    _.ValidationRules = new[] {new RequiresAuthValidationRule()}.Concat(DocumentValidator.CoreRules());
});
```

# Subscriptions

The Schema class supports a Subscription graph type and the parser supports the `subscription` keyword.  Subscriptions are an experimental feature of the GraphQL specification.

```graphql
subscription comments($repoName: String!) {
  newComments(repoName: $repoName) {
    content
    postedBy {
      username
    }
    postedAt
  }
}
```

# Schema Generation

There is currently nothing in the core project to do GraphQL Schema generation based off of existing C# classes.  Here are a few community projects built with GraphQL .NET which do so.

* [GraphQL Conventions](https://github.com/graphql-dotnet/conventions) by [Tommy Lillehagen](https://github.com/tlil87)
* [GraphQL Annotations](https://github.com/dlukez/graphql-dotnet-annotations) by [Daniel Zimmermann](https://github.com/dlukez)
* [GraphQL Schema Generator](https://github.com/holm0563/graphql-schemaGenerator) by [Derek Holmes](https://github.com/holm0563)

# How do I use XYZ ORM/database with GraphQL.NET?

* [Entity Framework](https://github.com/JacekKosciesza/StarWars) by [Jacek Kościesza](https://github.com/JacekKosciesza)
* [Marten + Nancy](https://github.com/joemcbride/marten/blob/graphql2/src/DinnerParty/Modules/GraphQLModule.cs) by [Joe McBride](https://github.com/joemcbride)
