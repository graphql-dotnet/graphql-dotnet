# GraphQL for .NET

[![Build Status](https://ci.appveyor.com/api/projects/status/github/graphql-dotnet/graphql-dotnet?branch=master&svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/graphql-dotnet)
[![NuGet](https://img.shields.io/nuget/v/GraphQL.svg)](https://www.nuget.org/packages/GraphQL/)
[![Join the chat at https://gitter.im/graphql-dotnet/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/graphql-dotnet/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a work-in-progress implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

This project uses a [lexer/parser](http://github.com/graphql-dotnet/parser) originally written by [Marek Magdziak](https://github.com/mkmarek) and released with a MIT license.  Thank you Marek!

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL/).

`PM> Install-Package GraphQL`

## Upgrade Guide

* [0.11.0](/upgrade-guides/v0.11.0.md)
* [0.8.0](/upgrade-guides/v0.8.0.md)

## GraphiQL
There is a sample web api project hosting the GraphiQL interface.  `npm install` and build `webpack` from the root of the repository.

```
> npm install
> npm start
```
![](http://i.imgur.com/2uGdVAj.png)

## Usage

Define your type system with a top level query object.

```csharp
using GraphQL.Http;
using GraphQL.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class StarWarsSchema : Schema
{
    public StarWarsSchema()
    {
        Query = new StarWarsQuery(new StarWarsData());
    }
}

public class StarWarsQuery : ObjectGraphType
{
    public StarWarsQuery()
    {
    }

    public StarWarsQuery(StarWarsData data)
    {
        Name = "Query";

        Field<CharacterInterface>("hero", resolve: context => data.GetDroidByIdAsync("3"));
        Field<HumanType>(
            "human",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the human" }
            ),
            resolve: context => data.GetHumanByIdAsync(context.Argument<string>("id"))
        );
        Field<DroidType>(
            "droid",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the droid" }
            ),
            resolve: context => data.GetDroidByIdAsync(context.Argument<string>("id"))
        );
    }
}

public class CharacterInterface : InterfaceGraphType
{
    public CharacterInterface()
    {
        Name = "Character";
        Field<NonNullGraphType<StringGraphType>>("id", "The id of the character.");
        Field<NonNullGraphType<StringGraphType>>("name", "The name of the character.");
        Field<ListGraphType<CharacterInterface>>("friends");
    }
}

public class DroidType : ObjectGraphType
{
    public DroidType()
    {
        var data = new StarWarsData();
        Name = "Droid";
        Field<NonNullGraphType<StringGraphType>>("id", "The id of the droid.");
        Field<NonNullGraphType<StringGraphType>>("name", "The name of the droid.");
        Field<ListGraphType<CharacterInterface>>(
            "friends",
            resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
        );
        Interface<CharacterInterface>();
        IsTypeOf = value => value is Droid;
    }
}

public class HumanType : ObjectGraphType
{
    public HumanType()
    {
    }

    public HumanType(StarWarsData data)
    {
        Name = "Human";

        Field<NonNullGraphType<StringGraphType>>("id", "The id of the human.");
        Field<StringGraphType>("name", "The name of the human.");
        Field<ListGraphType<CharacterInterface>>(
            "friends",
            resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
        );
        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
        Field<StringGraphType>("homePlanet", "The home planet of the human.");

        Interface<CharacterInterface>();

        IsTypeOf = value => value is Human;
    }
}

public class StarWarsData
{
    private readonly List<Human> _humans = new List<Human>();
    private readonly List<Droid> _droids = new List<Droid>();

    public StarWarsData()
    {
        _humans.Add(new Human
        {
            Id = "1", Name = "Luke",
            Friends = new[] {"3", "4"},
            AppearsIn = new[] {4, 5, 6},
            HomePlanet = "Tatooine"
        });
        _humans.Add(new Human
        {
            Id = "2", Name = "Vader",
            AppearsIn = new[] {4, 5, 6},
            HomePlanet = "Tatooine"
        });

        _droids.Add(new Droid
        {
            Id = "3", Name = "R2-D2",
            Friends = new[] {"1", "4"},
            AppearsIn = new[] {4, 5, 6},
            PrimaryFunction = "Astromech"
        });
        _droids.Add(new Droid
        {
            Id = "4", Name = "C-3PO",
            AppearsIn = new[] {4, 5, 6},
            PrimaryFunction = "Protocol"
        });
    }

    public IEnumerable<StarWarsCharacter> GetFriends(StarWarsCharacter character)
    {
        if (character == null)
        {
            return null;
        }

        var friends = new List<StarWarsCharacter>();
        var lookup = character.Friends;
        if (lookup != null)
        {
            _humans.Where(h => lookup.Contains(h.Id)).Apply(friends.Add);
            _droids.Where(d => lookup.Contains(d.Id)).Apply(friends.Add);
        }
        return friends;
    }

    public Task<Human> GetHumanByIdAsync(string id)
    {
        return Task.FromResult(_humans.FirstOrDefault(h => h.Id == id));
    }

    public Task<Droid> GetDroidByIdAsync(string id)
    {
        return Task.FromResult(_droids.FirstOrDefault(h => h.Id == id));
    }
}

public abstract class StarWarsCharacter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string[] Friends { get; set; }
    public int[] AppearsIn { get; set; }
}

public class Human : StarWarsCharacter
{
    public string HomePlanet { get; set; }
}

public class Droid : StarWarsCharacter
{
    public string PrimaryFunction { get; set; }
}

public class EpisodeEnum : EnumerationGraphType
{
    public EpisodeEnum()
    {
        Name = "Episode";
        Description = "One of the films in the Star Wars Trilogy.";
        AddValue("NEWHOPE", "Released in 1977.", 4);
        AddValue("EMPIRE", "Released in 1980.", 5);
        AddValue("JEDI", "Released in 1983.", 6);
    }
}

public enum Episodes
{
    NEWHOPE = 4,
    EMPIRE = 5,
    JEDI = 6
}
```

Executing a query.

```csharp
public async Task<string> Execute(
  Schema schema,
  object rootObject,
  string query,
  string operationName = null,
  Inputs inputs = null)
{
  var executer = new DocumentExecuter();
  var writer = new DocumentWriter();

  var result = await executer.ExecuteAsync(schema, rootObject, query, operationName, inputs);
  return writer.Write(result);
}

var schema = new StarWarsSchema();

var query = @"
  query HeroNameQuery {
    hero {
      name
    }
  }
";

var result = await Execute(schema, null, query);

Console.Writeline(result);

// prints
{
  "data": {
    "hero": {
      "name": "R2-D2"
    }
  }
}
```

## Roadmap

### Grammar / AST
- Grammar and AST for the GraphQL language should be complete.

### Operation Execution
- [x] Scalars
- [x] Objects
- [x] Lists of objects/interfaces
- [x] Interfaces
- [x] Unions
- [x] Arguments
- [x] Variables
- [x] Fragments
- [x] Directives
  - [x] Include
  - [x] Skip
  - [x] Custom
- [x] Enumerations
- [x] Input Objects
- [x] Mutations
- [x] Subscriptions
- [x] Async execution

### Validation
- [x] Arguments of correct type
- [x] Default values of correct type
- [x] Fields on correct type
- [x] Fragments on composite types
- [x] Known argument names
- [x] Known directives
- [x] Known fragment names
- [x] Known type names
- [x] Lone anonymous operations
- [x] No fragment cycles
- [x] No undefined variables
- [x] No unused fragments
- [x] No unused variables
- [ ] Overlapping fields can be merged
- [x] Possible fragment spreads
- [x] Provide non-null arguments
- [x] Scalar leafs
- [x] Unique argument names
- [x] Unique fragment names
- [x] Unique input field names
- [x] Unique operation names
- [x] Unique variable names
- [x] Variables are input types
- [x] Variables in allowed position

### Schema Introspection
- [x] __typename
- [x] __type
  - [x] name
  - [x] kind
  - [x] description
  - [x] fields
  - [x] interfaces
  - [x] possibleTypes
  - [x] enumValues
  - [x] inputFields
  - [x] ofType
- [x] __schema
  - [x] types
  - [x] queryType
  - [x] mutationType
  - [x] subscriptionType
  - [x] directives

### Deployment Process

```
npm run setVersion 0.10.0
write release notes in release-notes.md
git commit/push
download nuget from AppVeyor
upload nuget package to github
upload nuget package to nuget.org
```
