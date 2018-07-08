<!--Title:Getting Started-->
<!--Url:getting-started-->

## Basics

As its name suggests, this library is a .NET implementation of [GraphQL](http://graphql.org/learn). This document assumes you are already familiar with the fundamentals of GraphQL.

```
+------------------------------------+
|           Interactivity            |
+------------------------------------+
+------------------------------------+
|         +-----------------------+  |
|         | Execution             |  |
|         +-----------------------+  |
| GraphQL | Queries and Mutations |  |
|         +-----------------------+  |
|         | Types and Interfaces  |  |
|         +-----------------------+  |
+------------------------------------+
+------------------------------------+
|                Data                |
+------------------------------------+
```

GraphQL sits between your data and the "outside world." This library only concerns itself with this middle layer. For simplicity, the documentation uses hard-coded data, but in a real-world application your data layer would consist of some sort of ORM (like Entity Framework) or other data provider. And as for interactivity, how specifically the query is received and the output returned is beyond the scope of this document. See the ["Further Reading"](#further-reading) section at the end of this document for examples and tutorials specific to various ORMs and deployment scenarios.

## Example

Here's a commented "Hello World" example.

GraphQL schema
```graphql
type StarWarsQuery {
  hero: Droid
}

type Droid {
  id: String!
  name: String
}
```

C# implementation
```csharp
namespace ConsoleApplication
{
    using System;
    using System.Threading.Tasks;
    using GraphQL;
    using GraphQL.Http;
    using GraphQL.Types;

    /*
     * Data Layer
     * In this example, it's a simple object. In the "real world,"
     * it would likely be an object tied to an ORM, like Entity Framework,
     * or a data transfer object tied to some other data provider.
     */
    public class Droid
    {
      public string Id { get; set; }
      public string Name { get; set; }
    }

    /*
     * Interface/Type Layer
     * Every type you wish to expose via GraphQL needs to be defined.
     * See the "Further Reading" section of the docs for ways of automatically
     * generating GraphQL code from existing data objects.
     */
    public class DroidType : ObjectGraphType<Droid>
    {
      public DroidType()
      {
        Field(x => x.Id).Description("The Id of the Droid.");
        Field(x => x.Name, nullable: true).Description("The name of the Droid.");
      }
    }

    /*
     * Query/Mutation Layer
     * The root query is the entry point of your GraphQL service.
     * You must have one, and only one, root query object.
     * This object gathers together all your defined types and configures the types
     * of searches and filters you support.
     *
     * In this example, the data itself is hardcoded in the `resolve:` line.
     * In the "real world," you'd be querying your data provider.
     */
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
          Run().Wait();
        }

        /*
         * Interactivity Layer
         * First you generate the schema using your root query object.
         * Then you use `DocumentExecuter` to run the query against that schema.
         * How the query is received or the results returned is beyond the scope
         * of the library.
         */
        private static async Task Run()
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

## Types & Interfaces

### Types

Types are the building blocks of your GraphQL service. Let's go back to our "Hello World" example:

```graphql
type Droid {
  id: String!
  name: String
}
```

```csharp
public class DroidType : ObjectGraphType<Droid>
{
  public DroidType()
  {
    Field(x => x.Id).Description("The Id of the Droid.");
    Field(x => x.Name, nullable: true).Description("The name of the Droid.");
  }
}
```

> TODO: Talk about the `Field` function and the various ways it can be used (e.g., `Field<Type>(...)`). Be sure to mention the default resolver. Also, there appears to be some case transformation of the property name happening in the background (e.g., "Id" automatically becoming "id").

> TODO: Be sure to introduce `ListGraphType`!

> TODO: Introduce `context.Source` and how that functions.

> TODO: Give some examples of more-complex fields—for example, using `Field<Type>(...)` and `resolve:` to transform types or pull information from child objects.

The type's class must inherit from `ObjectGraphType<T>` where `T` is the class from the data layer (ORM object or other sort of data transfer object).

You define fields using the simple `Field()` syntax or the more granular `Field<T>()` syntax. For most fields you can just mimic the example code: point to the property,  give it a helpful description, and be done. But for more complex scenarios, you'll need the full syntax.

`Field<TGraph>("NAME", resolve: context => ..., description: "Some sort of description");`

`TGraph` should be the type of the field. The basic types are `BooleanGraphType`, `DateGraphType`, `DecimalGraphType`, `EnumerationGraphType`, `FloatGraphType`, `IntGraphType`, and `StringGraphType`, among others.

> TODO: But I see these are marked as "obsolete" in the source code, so...

The first parameter is the name of the field (what users will use in their GraphQL queries). Then, most importantly, you need some sort of resolver. In the basic case, the system just fetches the value of the given property. But in these more complex cases, you will need to provide specific code.

#### Transformed or Calculated Values

What if the value in the database is binary but you want to transform it into a string when it is fetched?

`Field<StringGraphType>("bytecode", resolve: context => ((T)context.Source).ByteCode.ToString(), description: "Some sort of description");`

In the resolver, we use the `Source` property of the parameter (`context`) to refer to the underlying data object (of type `T`). We then access the `ByteCode` property and convert it to a string.

You could use this same approach to calculate the value:

`Field<StringGraphType>("fullName", resolve: context => ((T)context.Source).FirstName + ((T)context.Source).LastName, description: "User's full name");`

#### List Fields

A common need is to return a list of items. You do this using the `ListGraphType`.

Imagine a student class (`Student`) with a property containing all the courses they're currently taking (`Courses`). You must also have defined a GraphQL type representing courses (`CourseType`). You could return those as follows:

`Field<ListGraphType<CourseType>>("currentCourses", resolve: context => ((Student)context.Source).Courses.ToArray(), description: "Courses this student is enrolled in");`

### Interfaces

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

#### Resolving Ambiguity

When you have a field that returns a GraphQL Interface, the engine needs to know which concrete graph type to use. So if you have a Character interface that is implemented by both Human and Droid types, the engine needs to know which graph type to choose. There are two ways of resolving this ambiguity: at the interface level or at the type level.

> TODO: Give some indication of the pros and cons of the various approaches. Are there performance implications, for example? Why choose one over the other?

> TODO: I'm also not clear why we're talking about this at all. If `ObjectGraphType<Type>` gives you a default `IsTypeOf`, why would anybody need to understand how this works? Are there situations where the default `IsTypeOf` doesn't work? If so, what are those situations? Can we just delete this whole "Resolving Ambiguity" section?

##### Interface Level (`ResolveType`)

`ResolveType` must be exhaustive. If you add another type that implements the interface, you must add it to the `ResolveType` code.

> If a type implements `ResolveType` then any `IsTypeOf` implementation is ignored.

```csharp
public class CharacterInterface : InterfaceGraphType<StarWarsCharacter>
{
  public CharacterInterface(
    //TODO: The following two lines need to be explained. What are these?
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

##### Type Level (`IsTypeOf`)

The data object being mapped is passed to the `IsTypeOf` function which should return a boolean value.

```csharp
//TODO: Isn't the following line missing the <Type> after ObjectGraphType?
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


### Unions

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

## Queries & Mutations

### Arguments

Before we get into the queries and mutations themselves, you need to understand how arguments can be passed. 

In GraphQL, you can provide arguments to a field as follows:

```graphql
query {
  droid(id: "1") {
    id
    name
  }
}
```

You can use `GetArgument` on `ResolveFieldContext` to retrieve argument values.  `GetArgument` will attempt to coerce the argument values to the generic type it is given, including primitive values, objects, and enumerations.  You can gain access to the value directly through the `Arguments` dictionary on `ResolveFieldContext`.


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
        //TODO: What is the following line doing here? Is it an alternative? Or what?
        var objectId = context.Arguments["id"];
        return data.GetDroidByIdAsync(id);
      }
    );
  }
}
```

> TODO: We need to introduce optional (nullable) arguments.

### Queries

The query is your service's primary entry point. You must have one, and only one, root Query object that is an `ObjectGraphType`.  Queries must only fetch data and never modify it.

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
```

> TODO: Needs to be expanded to demonstrate basics like how to search and return multiple results. That's going to include adding an explanation of passing a database context to the constructor.

Let's look at an example that connects to an actual data source. For that to work, you need to pass some sort of encapsulating object into the query, like an Entity Framework "context" object. Let's make it possible to search for a specific individual user or to get a list of all users.

```csharp
public class MyQuery : ObjectGraphType
{
  public MyQuery(MyContext db)
  {
    Field<UserType>(
      "user",
      arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "id" }),
      resolve: _ =>
      {
          var id = _.GetArgument<int>("id");
          return db.Users.Single(x => x.Id.Equals(id));
      }
    );
    Field<ListGraphType<UserType>>(
      "users",
      resolve: _ =>
      {
        return db.Users.ToArray();
      }
    );
  }
}
```

This would support queries like the following:

```graphql
query {
  user(id: 1) {
    id
    name
  }
}

query {
  users {
    id
    name
  }
}
```

### Mutations

To perform a mutation you need to have a root Mutation object that is an `ObjectGraphType`.  Mutations make modifications to data and return a result.  You can only have a single root Mutation object.

Mutations are a little more complex. The basic approach is as follows:

* Define an `InputObjectGraphType` class that defines what input the mutation requires.
* Define a data transfer object (DTO) class to receive the input.
* Define an `ObjectGraphType` class that represents the mutator. This is where the input is processed, the mutation executed, and final data returned.

Here's a very simple example that creates a user with a given name (required) and an optional tagline. 

```csharp
public class UserInputType : InputObjectGraphType
{
  /*
   * Note the use of `NonNullGraphType`. Use this to mark
   * fields as required.
   */
  public UserInputType()
  {
    Name = "UserInput";
    Field<NonNullGraphType<StringGraphType>>("name");
    Field<StringGraphType>("tagline");
  }
}

/*
 * All that matters is that the class have gettable and settable attributes
 * for each of the input fields. Nothing else is needed.
 */
public class UserDTO
{
  public string name {get; set;}
  public string tagline {get; set;}
}

public class MyMutator : ObjectGraphType
{
  public MyMutator(MyContext db)
  {
    //`UserType` is the GraphQL type for your user object that you created earlier
    Field<UserType>(
      "createUser",
      arguments: new QueryArguments(
        /*
         * Best practice is to pass everything through a single `input` field
         * rather than passing each field individually. It's more maintainable.
         */
        new QueryArgument<NonNullGraphType<UserInputType>> {Name = "input"}
      ),
      resolve: _ => {
        var profile = _.GetArgument<UserDTO>("input");
        /*
         * Use the data now stored in `profile` to populate a new record.
         * Return the record and the library will convert it to `UserType`
         * if everything was done right.
         */
        var newrec = //magic goes here
        return newrec;
      }
    );
  }
}
```

> TODO: And how are errors handled? What if something happens during the `AddHuman` part in the database layer? Will a meaningful error message be returned?

See the [Execution](#execution) section for more information on how to accept input cleanly.

### Subscriptions

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

## Execution

So we've established interfaces and types that represent the various types of information we expose. We then created queries and mutations that work with these types in various ways. Now we're ready to actually process a query.

### Schema Generation

First you need a schema, which is a class that inherits `Schema`. It has two properties:

* `Query` (required): This is the root query object.
* `Mutation` (optional): This is the root mutation object.

And you'll need to pass in some sort of object that encapsulates your data provider. In this case, it's an Entity Framework "context" object.

```csharp
// Read only: No mutation requests are supported
public class MySchemaRO : Schema
{
  public MySchemaRO(MyContext db)
  {
    Query = new MyQuery(db);
  }
}

// Read/Write: Both queries and mutations are supported
public class MySchemaRW : Schema
{
  public MySchemaRW(MyContext db)
  {
    Query = new MyQuery(db);
    Mutation = new MyMutator(db);
  }
}
```

#### RegisterType

When the Schema is built, it looks at the "root" types (Query, Mutation, Subscription) and gathers all of the GraphTypes they expose. Often when you are working with an interface type the concrete types are not exposed on the root types (or any of their children). Since those concrete types are never exposed in the type graph the Schema doesn't know they exist. This is what the `RegisterType<>` method on the Schema is for.  By using `RegisterType<>`, it tells the Schema about the specific type and it will properly add it to the `PossibleTypes` collection on the interface type when the Schema is initialized.

```csharp
public class StarWarsSchema : Schema
{
  public StarWarsSchema()
  {
    Query = new StarWarsQuery();
    RegisterType<DroidType>();
  }
}
```

### `DocumentExecuter`

> TODO: Needs expanding. What are the various inputs? Does it *have* to be asynchronous? How does it handle errors? What level of granularity can one expect in the error messages?

> TODO: Needs to include `ExposeExceptions`.

`DocumentExecuter` is an `async` function that executes the query against your schema. Here's a simple example also uses the `ExposeExceptions` property, which will pass any exceptions and stack traces to the final output, useful during development.

```csharp
string query = //get from incoming request
var result = await new DocumentExecuter().ExecuteAsync(_ =>
{
  _.Schema = new MySchemaRO(dbObj);
  _.Query = query;
  _.ExposeExceptions = true;
}).ConfigureAwait(false);
```

You can pass variables received from the client to the execution engine by using the `Inputs` property.

* See the [official GraphQL documentation on variables](http://graphql.org/learn/queries/#variables)
* See also the [offical GraphQL documentation on serving queries over HTTP](https://graphql.org/learn/serving-over-http/)

Here is what a query looks like with a variable:

```graphql
query DroidQuery($droidId: String!) {
  droid(id: $droidId) {
    id
    name
  }
}
```

Here is what this query would look like as a JSON request:

```json
{
 "query": "query DroidQuery($droidId: String!) { droid(id: $droidId) { id name } }",
 "variables": {
   "droidId": "1"
 }
}
```

```csharp
string variablesJson = // get from request
// `ToInputs` converts the json to the `Inputs` class
var inputs = variablesJson.ToInputs();

var result = await executer.ExecuteAsync(_ =>
{
    _.Inputs = inputs;
});
```

### Input Validation

There [are a number of query validation rules](http://facebook.github.io/graphql/#sec-Validation) that are ran when a query is executed.  All of these are turned on by default.  You can add your own validation rules or clear out the existing ones by accessing the `ValidationRules` property.

```csharp
var result = await executer.ExecuteAsync(_ =>
{
    _.ValidationRules = new[] {new RequiresAuthValidationRule()}.Concat(DocumentValidator.CoreRules());
});
```

## Further Reading

### GraphiQL

[GraphiQL](https://github.com/graphql/graphiql) is an interactive in-browser GraphQL IDE.  This is a fantastic developer tool to help you form queries and explore your Schema.  The [sample project](https://github.com/graphql-dotnet/examples/tree/master/src/AspNetCoreCustom) gives an example of hosting the GraphiQL IDE.

![](http://i.imgur.com/2uGdVAj.png)

### Sample Code

### Tutorials

### Schema Generation

There is currently nothing in the core project to do GraphQL Schema generation based off of existing C# classes.  Here are a few community projects built with GraphQL .NET which do so.

* [GraphQL Conventions](https://github.com/graphql-dotnet/conventions) by [Tommy Lillehagen](https://github.com/tlil87)
* [GraphQL Annotations](https://github.com/dlukez/graphql-dotnet-annotations) by [Daniel Zimmermann](https://github.com/dlukez)
* [GraphQL Schema Generator](https://github.com/holm0563/graphql-schemaGenerator) by [Derek Holmes](https://github.com/holm0563)

### How do I use XYZ ORM/database with GraphQL.NET?

* [Entity Framework](https://github.com/JacekKosciesza/StarWars) by [Jacek Kościesza](https://github.com/JacekKosciesza)
* [Marten + Nancy](https://github.com/joemcbride/marten/blob/graphql2/src/DinnerParty/Modules/GraphQLModule.cs) by [Joe McBride](https://github.com/joemcbride)
