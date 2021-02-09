# GraphQL for .NET

[![Join the chat at https://gitter.im/graphql-dotnet/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/graphql-dotnet/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build status](https://github.com/graphql-dotnet/graphql-dotnet/workflows/Build%20artifacts/badge.svg)](https://github.com/graphql-dotnet/graphql-dotnet/actions?query=workflow%3A%22%22Build+artifacts%22%22)
[![Build status](https://github.com/graphql-dotnet/graphql-dotnet/workflows/Publish%20code/badge.svg)](https://github.com/graphql-dotnet/graphql-dotnet/actions?query=workflow%3A%22%22Publish+code%22%22)
[![CodeQL analysis](https://github.com/graphql-dotnet/graphql-dotnet/workflows/CodeQL%20analysis/badge.svg)](https://github.com/graphql-dotnet/graphql-dotnet/actions?query=workflow%3A%22%22CodeQL+analysis%22%22)

[![Total alerts](https://img.shields.io/lgtm/alerts/g/graphql-dotnet/graphql-dotnet.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/graphql-dotnet/graphql-dotnet/alerts/)
[![Language grade: C#](https://img.shields.io/lgtm/grade/csharp/g/graphql-dotnet/graphql-dotnet.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/graphql-dotnet/graphql-dotnet/context:csharp)

[![Backers on Open Collective](https://opencollective.com/graphql-net/backers/badge.svg)](#backers)
[![Sponsors on Open Collective](https://opencollective.com/graphql-net/sponsors/badge.svg)](#sponsors) 

[![NuGet](https://img.shields.io/nuget/v/GraphQL)](https://www.nuget.org/packages/GraphQL)
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

1. http://graphql-dotnet.github.io - documentation site that is built from the [docs](/docs2/site/) folder in the `master` branch.
2. https://graphql.org/learn - learn about GraphQL, how it works, and how to use it.

## Debugging

All packages generated from this repository come with embedded pdb and support [Source Link](https://github.com/dotnet/sourcelink).
If you are having difficulty understanding how the code works or have encountered an error, then it is just enough to enable
Source Link in your IDE settings. Then you can debug GraphQL.NET source code as if it were part of your project.

## Installation

You can install the latest stable version via [NuGet](https://www.nuget.org/packages/GraphQL/).
```
> dotnet add package GraphQL
```

For serialized results, you'll need an `IDocumentWriter` implementation.
We support several serializers (or you can bring your own):

| Package | Downloads | Nuget Latest |
|---------|-----------|--------------|
| GraphQL.SystemTextJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson) |
| GraphQL.NewtonsoftJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) |

```
> dotnet add package GraphQL.SystemTextJson
> dotnet add package GraphQL.NewtonsoftJson
```
> *Note: You can use `GraphQL.NewtonsoftJson` with .NET Core 3+, just be aware it lacks async writing 
> capabilities so writing to an ASP.NET Core 3.0 `HttpResponse.Body` will require you to set 
> `AllowSynchronousIO` to `true` as per [this announcement](https://github.com/aspnet/Announcements/issues/342);
> which isn't recommended.*

You can get all preview versions from [GitHub Packages](https://github.com/orgs/graphql-dotnet/packages?repo_name=graphql-dotnet).
Note that GitHub requires authentication to consume the feed.  See [here](https://docs.github.com/en/free-pro-team@latest/packages/publishing-and-managing-packages/about-github-packages#authenticating-to-github-packages).

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

* [API Development in .NET with GraphQL](https://www.lynda.com/NET-tutorials/API-Development-NET-GraphQL/664823-2.html) - [Glenn Block](https://twitter.com/gblock) demonstrates how to use the GraphQL.NET framework to build a fully functional GraphQL endpoint.
* [Building GraphQL APIs with ASP.NET Core](https://app.pluralsight.com/library/courses/building-graphql-apis-aspdotnet-core/table-of-contents) by [Roland Guijt](https://github.com/RolandGuijt)

## Upgrade Guides

You can see the changes in public APIs using [fuget.org](https://www.fuget.org/packages/GraphQL/3.0.0/lib/netstandard2.0/diff/2.4.0/).

* [3.x to 4.x - under development](https://github.com/graphql-dotnet/graphql-dotnet/blob/develop/docs2/site/docs/guides/migration4.md)
* [2.4.x to 3.x](https://graphql-dotnet.github.io/docs/migrations/migration3)
* [0.17.x to 2.x](https://graphql-dotnet.github.io/docs/migrations/migration2)
* [0.11.0](https://graphql-dotnet.github.io/docs/migrations/v0_11_0)
* [0.8.0](https://graphql-dotnet.github.io/docs/migrations/v0_8_0)

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

This example uses the [GraphQL schema language](https://graphql.org/learn/schema/#type-language).
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


## Publishing NuGet packages

The package publishing process is automated with [GitHub Actions](https://github.com/features/actions).

After your PR is merged into `master` or `develop`, preview packages are published to [GitHub Packages](https://github.com/orgs/graphql-dotnet/packages?repo_name=graphql-dotnet).

Stable versions of packages are published to NuGet when a [release](https://github.com/graphql-dotnet/graphql-dotnet/releases) is created.

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
