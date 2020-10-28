# GraphQL for .NET

[![Build Status](https://ci.appveyor.com/api/projects/status/github/graphql-dotnet/graphql-dotnet?branch=master&svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/graphql-dotnet)
[![Join the chat at https://gitter.im/graphql-dotnet/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/graphql-dotnet/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Backers on Open Collective](https://opencollective.com/graphql-net/backers/badge.svg)](#backers) [![Sponsors on Open Collective](https://opencollective.com/graphql-net/sponsors/badge.svg)](#sponsors) 

[![NuGet](https://img.shields.io/nuget/v/GraphQL)](https://www.nuget.org/packages/GraphQL)
[![Nuget](https://img.shields.io/nuget/vpre/GraphQL)](https://www.nuget.org/packages/GraphQL)
[![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json)
[![Nuget](https://img.shields.io/nuget/dt/GraphQL)](https://www.nuget.org/packages/GraphQL)

![Activity](https://img.shields.io/github/commit-activity/w/graphql-dotnet/graphql-dotnet)
![Activity](https://img.shields.io/github/commit-activity/m/graphql-dotnet/graphql-dotnet)
![Activity](https://img.shields.io/github/commit-activity/y/graphql-dotnet/graphql-dotnet)

![Size](https://img.shields.io/github/repo-size/graphql-dotnet/graphql-dotnet)

This is an implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

Now the [specification](https://github.com/graphql/graphql-spec) is being developed by the
[GraphQL Foundation](https://foundation.graphql.org/).

This project uses a [lexer/parser](http://github.com/graphql-dotnet/parser) originally written
by [Marek Magdziak](https://github.com/mkmarek) and released with a MIT license. Thank you Marek!

## Documentation

http://graphql-dotnet.github.io

> *Note: The current state of documentation corresponds to the state of the code in the master branch.

## Installation

> WARNING: The latest stable version 2.4.0 has many known issues that have been fixed in 3.0.0-preview-XXXX versions.
> If errors occur, it is recommended that you first check the behavior on the latest available preview version before
> reporting a issue. Latest 3.0.0-preview-XXXX versions are **backwards incompatible** with latest stable 2.4.0 version.
> You can see the changes in public APIs using [fuget.org](https://www.fuget.org/packages/GraphQL/3.0.0-preview-1490/lib/netstandard2.0/diff/2.4.0/).

You can install the latest stable version via [NuGet](https://www.nuget.org/packages/GraphQL/).
```
> dotnet add package GraphQL
```

For serialized results, you'll need an `IDocumentWriter` implementation.
We support several serializers (or you can bring your own):

| Package | Downloads | Nuget Latest | MyGet Latest |
|---------|-----------|--------------|--------------|
| GraphQL.SystemTextJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson/) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.SystemTextJson?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.NewtonsoftJson, formerly included in [GraphQL](https://www.nuget.org/packages/GraphQL/) | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.NewtonsoftJson?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |

```
> dotnet add package GraphQL.SystemTextJson
> dotnet add package GraphQL.NewtonsoftJson
```
> *Note: You can use `GraphQL.NewtonsoftJson` with .NET Core 3+, just be aware it lacks async writing 
> capabilities so writing to an ASP.NET Core 3.0 `HttpResponse.Body` will require you to set 
> `AllowSynchronousIO` to `true` as per [this announcement](https://github.com/aspnet/Announcements/issues/342);
> which isn't recommended.*

You can get the latest pre-release packages from the [MyGet feed](src/NuGet.config),
where you may want to explicitly pull a certain version using `-v`.
```
> dotnet add package GraphQL.SystemTextJson -v 3.0.0-preview-1593
```

[MyGet feed](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) is the primary source of packages
where you can find all preview versions built from the `master` branch. Periodically (usually once every
few months) the latest preview version is published to NuGet manually. This is due to fairly frequent
changes. Publication of each preview version to NuGet would create only unnecessary noise.

## Examples

https://github.com/graphql-dotnet/examples

You can also try an example of GraphQL demo server inside this repo - [GraphQL.Harness](src/GraphQL.Harness/GraphQL.Harness.csproj).
It supports the popular IDEs for managing GraphQL requests and exploring GraphQL schema:
- [Altair](https://github.com/imolorhe/altair)
- [Firecamp](https://firecamp.io/graphql/)
- [GraphiQL](https://github.com/graphql/graphiql)
- [GraphQL Playground](https://github.com/prisma-labs/graphql-playground)
- [Voyager](https://github.com/APIs-guru/graphql-voyager)

## Training

* [API Development in .NET with GraphQL](https://www.lynda.com/NET-tutorials/API-Development-NET-GraphQL/664823-2.html) - [Glenn Block](https://twitter.com/gblock) demonstrates how to use the GraphQL .NET framework to build a fully functional GraphQL endpoint.
* [Building GraphQL APIs with ASP.NET Core](https://app.pluralsight.com/library/courses/building-graphql-apis-aspdotnet-core/table-of-contents) by [Roland Guijt](https://github.com/RolandGuijt)

## Upgrade Guides

* 2.4.0 to 3.0 - under development
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
var json = await schema.ExecuteAsync(_ =>
{
  _.Query = "{ hello }";
  _.Root = root;
});

Console.WriteLine(json);
```

### Schema First Approach

This example uses the [Graphql schema language](https://graphql.org/learn/schema/#type-language).
See the [documentation](https://graphql-dotnet.github.io/docs/getting-started/introduction) for
more examples and information.

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

var json = await schema.ExecuteAsync(_ =>
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

var json = await schema.ExecuteAsync(_ =>
{
  _.Query = $"{{ droid(id: \"123\") {{ id name }} }}";
});
```

## Roadmap

### Grammar / AST
- Grammar and AST for the GraphQL language should be compatible with the [June 2018 specification](https://graphql.github.io/graphql-spec/June2018/).

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
To run this project on OSX with mono you will need to add some configuration.
Make sure mono is installed and add the following to your bash configuration:

```bash
export FrameworkPathOverride=/Library/Frameworks/Mono.framework/Versions/4.6.2/lib/mono/4.5/
```

See the following for more details:

* [Building VS 2017 MSBuild csproj Projects with Mono on Linux](https://stackoverflow.com/questions/42747722/building-vs-2017-msbuild-csproj-projects-with-mono-on-linux)
* [using .NET Framework as targets framework, the osx/unix build fails](https://github.com/dotnet/netcorecli-fsc/wiki/.NET-Core-SDK-rc4#using-net-framework-as-targets-framework-the-osxunix-build-fails)

## Contributors

This project exists thanks to all the people who contribute. 
<a href="https://github.com/graphql-dotnet/graphql-dotnet/graphs/contributors"><img src="https://opencollective.com/graphql-net/contributors.svg?width=890&button=false" /></a>


## Backers

Thank you to all our backers! 🙏 [Become a backer](https://opencollective.com/graphql-net#backer).

<a href="https://opencollective.com/graphql-net#backers" target="_blank"><img src="https://opencollective.com/graphql-net/backers.svg?width=890"></a>


## Sponsors

Support this project by becoming a sponsor. Your logo will show up here with a link to your website. [Become a sponsor](https://opencollective.com/graphql-net#sponsor).

<a href="https://opencollective.com/graphql-net/sponsor/0/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/1/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/2/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/3/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/4/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/5/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/6/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/6/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/7/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/8/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/9/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/9/avatar.svg"></a>


