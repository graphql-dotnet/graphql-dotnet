# GraphQL for .NET

[![Build Status](https://ci.appveyor.com/api/projects/status/github/graphql-dotnet/graphql-dotnet?branch=master&svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/graphql-dotnet)
[![NuGet](https://img.shields.io/nuget/v/GraphQL.svg)](https://www.nuget.org/packages/GraphQL/)
[![Join the chat at https://gitter.im/graphql-dotnet/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/graphql-dotnet/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is an implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

This project uses a [lexer/parser](http://github.com/graphql-dotnet/parser) originally written by [Marek Magdziak](https://github.com/mkmarek) and released with a MIT license.  Thank you Marek!

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL/).

```
PM> Install-Package GraphQL
```

Or you can get the latest pre-release packages from the [MyGet feed](https://www.myget.org/F/graphql-dotnet/api/v3/index.json).


## Documentation

http://graphql-dotnet.github.io

## Examples

https://github.com/graphql-dotnet/examples

## Training

* [API Development in .NET with GraphQL](https://www.lynda.com/NET-tutorials/API-Development-NET-GraphQL/664823-2.html) - [Glenn Block](https://twitter.com/gblock) demonstrates how to use the GraphQL .NET framework to build a fully functional GraphQL endpoint.

## Upgrade Guides

* [0.17.x to 2.x](https://graphql-dotnet.github.io/docs/guides/migration)
* [0.11.0](/upgrade-guides/v0.11.0.md)
* [0.8.0](/upgrade-guides/v0.8.0.md)

## Basic Usage

Define your schema with a top level query object then execute that query.

Fully-featured examples can be found [here](https://github.com/graphql-dotnet/examples).

### Hello World

```csharp
var schema = Schema.For(@"
  type Query {
    hello: String
  }
");

var root = new { Hello = "Hello World!" };
var json = schema.Execute(_ =>
{
  _.Query = "{ hello }";
  _.Root = root;
});

Console.WriteLine(json);
```

### Schema First Approach

This example uses the [Graphql schema language](https://graphql.org/learn/schema/#type-language).  See the [documentation](https://graphql-dotnet.github.io/docs/getting-started/introduction) for more examples and information.

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  [GraphQLMetadata("droid")]
  public Droid GetDroid()
  {
    return new Droid { Id = "123", Name = "R2-D2" };
  }
}

var schema = Schema.For(@"
  type Droid {
    id: ID
    name: String
  }

  type Query {
    droid: Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = schema.Execute(_ =>
{
  _.Query = "{ droid { id name } }";
});
```

### Parameters

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  private List<Droid> _droids = new List<Droid>
  {
    new Droid { Id = "123", Name = "R2-D2" }
  };

  [GraphQLMetadata("droid")]
  public Droid GetDroid(string id)
  {
    return _droids.FirstOrDefault(x => x.Id == id);
  }
}

var schema = Schema.For(@"
  type Droid {
    id: ID
    name: String
  }

  type Query {
    droid(id: ID): Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = schema.Execute(_ =>
{
  _.Query = $"{{ droid(id: \"123\") {{ id name }} }}";
});
```

## Roadmap

### Grammar / AST
- Grammar and AST for the GraphQL language should be compatible with the [June 2018 specification](http://facebook.github.io/graphql/June2018/).

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
- [x] Overlapping fields can be merged
- [x] Possible fragment spreads
- [x] Provide non-null arguments
- [x] Scalar leafs
- [x] Unique argument names
- [x] Unique directives per location
- [x] Unique fragment names
- [x] Unique input field names
- [x] Unique operation names
- [x] Unique variable names
- [x] Variables are input types
- [x] Variables in allowed position
- [x] Single root field

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


### Publishing Nugets

```
yarn run setVersion 2.0.0
git commit/push
download nuget from AppVeyor
upload nuget package to github
publish nuget from MyGet
```

### Running on OSX with mono
To run this project on OSX with mono you will need to add some configuration.  Make sure mono is installed and add the following to your bash configuration:

```bash
export FrameworkPathOverride=/Library/Frameworks/Mono.framework/Versions/4.6.2/lib/mono/4.5/
```

See the following for more details:

* [Building VS 2017 MSBuild csproj Projects with Mono on Linux](https://stackoverflow.com/questions/42747722/building-vs-2017-msbuild-csproj-projects-with-mono-on-linux)
* [using .NET Framework as targets framework, the osx/unix build fails](https://github.com/dotnet/netcorecli-fsc/wiki/.NET-Core-SDK-rc4#using-net-framework-as-targets-framework-the-osxunix-build-fails)
