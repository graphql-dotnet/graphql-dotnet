# GraphQL for .NET

[![Build Status](https://ci.appveyor.com/api/projects/status/github/graphql-dotnet/graphql-dotnet?branch=master&svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/graphql-dotnet)
[![Join the chat at https://gitter.im/graphql-dotnet/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/graphql-dotnet/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a work-in-progress implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

This project uses [Antlr4](https://github.com/tunnelvisionlabs/antlr4cs) for the GraphQL grammar definition.

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL/).

`PM> Install-Package GraphQL`

## Upgrade Guide

0.8.0 - [upgrade guide](/upgrade-guides/v0.8.0.md)

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
    var data = new StarWarsData();
    Name = "Query";
    Field<CharacterInterface>(
      "hero",
      resolve: context => data.GetDroidById("3")
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
```

Executing a query.

```csharp
public string Execute(
  Schema schema,
  object rootObject,
  string query,
  string operationName = null,
  Inputs inputs = null)
{
  var executer = new DocumentExecuter();
  var writer = new DocumentWriter();

  var result = executer.Execute(schema, rootObject, query, operationName, inputs);
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

var result = Execute(schema, null, query);

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
- [x] Arguments
- [x] Variables
- [x] Fragments
- [x] Directives
- [x] Enumerations
- [x] Input Objects
- [x] Mutations
- [x] Unions
- [x] Async execution

### Validation
- [x] Arguments of correct type
- [ ] Default values of correct type
- [ ] Fields on correct type
- [ ] Fragments on composite type
- [ ] Known argument names
- [ ] Known directives
- [ ] Known fragment names
- [x] Known type names
- [x] Lone anonymous operations
- [ ] No fragment cycle
- [x] No undefined variables
- [ ] No unused fragments
- [ ] No unused variables
- [ ] Overlapping fields can be merged
- [ ] Possible fragment spreads
- [ ] Provide non-null arguments
- [x] Scalar leafs
- [ ] Unique argument names
- [ ] Unique fragment names
- [x] Unique input field names
- [x] Unique operation names
- [ ] Unique variable names
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
  - [ ] subscriptionType
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
