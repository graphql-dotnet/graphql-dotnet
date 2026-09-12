# Unions

Unions are a composition of two or more different types. To create such union type,
you need to inherit from `UnionGraphType` and call the `Type<TType>` method on
the all types that you want to include in this union.

```csharp
public class CatOrDog : UnionGraphType
{
  public CatOrDog()
  {
    Type<Cat>();
    Type<Dog>();
  }
}

public class Cat : ObjectGraphType<CatModel>
{
  public Cat()
  {
    Field<StringGraphType>("name");
    Field<BooleanGraphType>("meows");
  }
}

public class Dog : ObjectGraphType<DogModel>
{
  public Dog()
  {
    Field<StringGraphType>("name");
    Field<BooleanGraphType>("barks");
  }
}
```

In this example `CatOrDog` type should implement `ResolveType` or both `Cat` and
`Dog` types should implement `IsTypeOf`. Note that `IsTypeOf` is already implemented
for `ObjectGraphType<TSourceType>` so in this example `ResolveType` is not used.
For details about `IsTypeOf` and `ResolveType` see [Interfaces](../interfaces).

## C# unions

A C# `union` describes the same thing a GraphQL union does, a value that is exactly one of a
fixed set of types, so one maps directly onto the other. A field returning a union needs no
configuration at all:

```csharp
public record Cat(string Name, bool Meows);

public record Dog(string Name, bool Barks);

public union Pet(Cat, Dog);

public class Query
{
  public static Pet Pet => new(new Cat("Tibbles", true));
}
```

```graphql
union Pet = Cat | Dog

type Cat {
  name: String!
  meows: Boolean!
}

type Dog {
  name: String!
  barks: Boolean!
}
```

Each case type becomes a possible type of the GraphQL union, and the value a field returns is
the value held by the active case rather than the union itself, so it is selected through an
inline fragment in the usual way:

```graphql
{
  pet {
    __typename
    ... on Cat { name meows }
    ... on Dog { name barks }
  }
}
```

Two limitations follow from the GraphQL specification rather than from this library:

- A GraphQL union may only contain object types, so every case must map to one. A union such as
  `union IntOrString(int, string)` has no GraphQL representation, and is reported when the schema
  is initialized.
- GraphQL has no union input type, so a union used as an argument is reported as well.

`union` and `closed` are language features rather than runtime features, so they work on any
target framework that has a C# 15 compiler available, given the marker types the compiler lowers
them to. Those ship in the framework from .NET 11 onwards, and a polyfill supplies them below
that.
