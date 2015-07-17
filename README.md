# GraphQL for .NET

[![Join the chat at https://gitter.im/joemcbride/graphql-dotnet](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/joemcbride/graphql-dotnet?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a work-in-progress implementation of [Facebook's GraphQL](https://github.com/facebook/graphql) in .NET.

This project uses [Antlr4](https://github.com/tunnelvisionlabs/antlr4cs) for the GraphQL grammar definition.

## Installation

You can install the latest version via [NuGet](https://www.nuget.org/packages/GraphQL/).

`PM> Install-Package GraphQL`

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
    Field("id", "The id of the character.", NonNullGraphType.String);
    Field("name", "The name of the character.", NonNullGraphType.String);
    Field("friends", new ListGraphType<CharacterInterface>());
    ResolveType = (obj) =>
    {
      if (obj is Human)
      {
        return new HumanType();
      }
      return new DroidType();
    };
  }
}

public class DroidType : ObjectGraphType
{
  public DroidType()
  {
    var data = new StarWarsData();
    Name = "Droid";
    Field("id", "The id of the droid.", NonNullGraphType.String);
    Field("name", "The name of the droid.", NonNullGraphType.String);
    Field<ListGraphType<CharacterInterface>>(
        "friends",
        resolve: context => data.GetFriends(context.Source as StarWarsCharacter)
    );
    Interface<CharacterInterface>();
  }
}
```

Executing a query.

```csharp
public string Execute(
  Schema schema,
  string query,
  string operationName = null,
  Inputs inputs = null)
{
  var executer = new DocumentExecuter();
  var writer = new DocumentWriter();

  var result = executer.Execute(schema, query, operationName, inputs);
  return writer.Write(result);
}

var schema = new StartWarsSchema();

var query = @"
  query HeroNameQuery {
    hero {
      name
    }
  }
";

var result = Execute(schema, query);

Console.Writeline(result);

// prints
{
  "data": {
    "hero": {
      "name": "R2-D2"
    }
  }
  "errors": []
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
- [ ] Enumerations
- [ ] Input Objects
- [ ] Unions
- [ ] Async execution

### Validation
- Not started

### Schema Introspection
- [x] __typename
- [ ] __type
  - [x] name
  - [x] kind
  - [x] description
  - [x] fields
  - [x] interfaces
  - [ ] possibleTypes
  - [ ] enumValues
  - [ ] ofType
- [ ] __schema
  - [ ] types
  - [ ] queryType
  - [ ] mutationType
  - [ ] directives
