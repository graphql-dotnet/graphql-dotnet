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
There is a sample web api project hosting the GraphiQL interface.  `yarn install` and `yarn start` from the root of the repository, then run the web project from Visual Studio.

> Note: Before running the GraphiQL project: make sure you Build the entire solution so that all the project references get built. (GraphQL, GraphQL-Parser, etc) to avoid missing reference/assembly errors.


```
> npm install -g yarn
> yarn install
> yarn start
```
![](http://i.imgur.com/2uGdVAj.png)

## Usage

Define your schema with a top level query object then execute that query.

A more full-featured example including all classes required can be found [here](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/src/GraphQL.StarWars).

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

## Roadmap

### Grammar / AST
- Grammar and AST for the GraphQL language should be compatible with the April 2016 specification.

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
- [ ] Unique directives per location
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
yarn run setVersion 0.12.0
write release notes in release-notes.md
git commit/push
download nuget from AppVeyor
upload nuget package to github
publish nuget from MyGet
```
