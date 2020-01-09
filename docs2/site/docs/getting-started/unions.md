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
For details about `IsTypeOf` and `ResolveType` see [Interfaces](Interfaces).
