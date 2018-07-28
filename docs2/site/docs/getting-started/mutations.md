# Mutations

To perform a mutation you need to have a root Mutation object that is an `ObjectGraphType`.  Mutations make modifications to data and return a result.  You can only have a single root Mutation object.

* See the [StarWars example](https://github.com/graphql-dotnet/examples/tree/master/src/StarWars) for more details.
* See the [official GraphQL documentation on mutations](http://graphql.org/learn/queries/#mutations).

```csharp
public class StarWarsSchema : Schema
{
  public StarWarsSchema(IDependencyResolver resolver)
    : base(resolver)
  {
    Query = resolver.Resolve<StarWarsQuery>();
    Mutation = resolver.Resolve<StarWarsMutation>();
  }
}

/// <example>
/// This is an example JSON request for a mutation
/// {
///   "query": "mutation ($human:HumanInput!){ createHuman(human: $human) { id name } }",
///   "variables": {
///     "human": {
///       "name": "Boba Fett"
///     }
///   }
/// }
/// </example>
public class StarWarsMutation : ObjectGraphType
{
  public StarWarsMutation(StarWarsData data)
  {
    Field<HumanType>(
      "createHuman",
      arguments: new QueryArguments(
        new QueryArgument<NonNullGraphType<HumanInputType>> {Name = "human"}
      ),
      resolve: context =>
      {
        var human = context.GetArgument<Human>("human");
        return data.AddHuman(human);
      });
  }
}

public class HumanInputType : InputObjectGraphType
{
  public HumanInputType()
  {
    Name = "HumanInput";
    Field<NonNullGraphType<StringGraphType>>("name");
    Field<StringGraphType>("homePlanet");
  }
}

// in-memory data store
public class StarWarsData
{
  ...

  public Human AddHuman(Human human)
  {
    human.Id = Guid.NewGuid().ToString();
    _humans.Add(human);
    return human;
  }
}
```
