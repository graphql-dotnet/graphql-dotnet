# Interfaces

A GraphQL Interface is an abstract type that includes a certain set of fields that a type
must include to implement the interface.

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
    Field(d => d.Name).Description("The name of the character.");
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
    Field(d => d.Name).Description("The name of the droid.");

    Field<ListGraphType<CharacterInterface>>(
      "friends",
      resolve: context => data.GetFriends(context.Source)
    );
    Field(d => d.PrimaryFunction, nullable: true).Description("The primary function of the droid.");

    Interface<CharacterInterface>();
  }
}
```

## RegisterType

When the Schema is built, it looks at the "root" types (Query, Mutation, Subscription) and
gathers all of the GraphTypes they expose. Often when you are working with an interface type
the concrete types are not exposed on the root types (or any of their children). Since those
concrete types are never exposed in the type graph the Schema doesn't know they exist. This
is what the `RegisterType<>` method on the Schema is for.  By using `RegisterType<>`, it
tells the Schema about the specific type and it will properly add it to the `PossibleTypes`
collection on the interface type when the Schema is initialized.

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

## IsTypeOf

`IsTypeOf` is a function which helps resolve the implementing GraphQL type during execution.
For example, when you have a field that returns a GraphQL Interface the engine needs to know
which concrete Graph Type to use.  So if you have a `Character` interface that is implemented
by both `Human` and `Droid` types, the engine needs to know which graph type to choose.
The data object being mapped is passed to the `IsTypeOf` function which should return a boolean value.

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

> `ObjectGraphType<T>` provides a default implementation of `IsTypeOf` for you.

An alternate to using `IsTypeOf` is instead implementing `ResolveType` on the Interface
or Union. See the `ResolveType` section for more details.

## ResolveType

An alternate to using `IsTypeOf` is implementing `ResolveType` on the Interface or Union.
The major difference is `ResolveType` is required to be exhaustive.  If you add another type
that implements an Interface you are required to alter the Interface for that new type to be resolved.

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
