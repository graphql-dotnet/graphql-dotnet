# Schema Types

## Scalars

A GraphQL object type has a name and fields, but at some point those fields have to resolve to some concrete data. That's where the scalar types come in: they represent the leaves of the query.

These are the scalars provided by the [GraphQL Specification](https://graphql.github.io/graphql-spec/June2018/#sec-Scalars).

| GraphQL | GraphQL.NET        | .NET                    |
|----------|---------------------|-------------------------|
| `String`   | `StringGraphType`   | `string`                |
| `Int`       | `IntGraphType`      | `int`                   |
| `Float`     | `FloatGraphType`    | `double`                |
| `Boolean`   | `BooleanGraphType`  | `bool`                  |
| `ID`        | `IdGraphType`       | `int`, `long`, `string` |

> Note that you can use a `Guid` with `ID`.  It will just be serialized to a `string` and should be sent to your GraphQL Server as a `string`.

These are additional scalars provided by this project.

| GraphQL          | GraphQL.NET                    | .NET               | Format |
|------------------|---------------------------------|--------------------|-------|
| `Date`           | `DateGraphType`                 | `DateTime`         | ISO-8601: yyyy-MM-dd |
| `DateTime`       | `DateTimeGraphType`             | `DateTime`         | ISO-8601, assume UTC |
| `DateTimeOffset` | `DateTimeOffsetGraphType`       | `DateTimeOffset`   | ISO-8601 |
| `Seconds`        | `TimeSpanSecondsGraphType`      | `TimeSpan`         | number |
| `Milliseconds`   | `TimeSpanMillisecondsGraphType` | `TimeSpan`         | number |
| `Decimal` | `DecimalGraphType` | `decimal` | number |
| `Uri` | `UriGraphType` | `Uri` | RFC 2396/2732/3986/3987 |
| `Guid` | `GuidGraphType` | `Guid` | string |
| `Short` | `ShortGraphType` | `short` | number |
| `UShort` | `UShortGraphType` | `ushort` | number |
| `UInt` | `UIntGraphType` | `uint` | number |
| `Long` | `LongGraphType` | `long` | number |
| `ULong` | `ULongGraphType` | `ulong` | number |
| `Byte` | `ByteGraphType` | `byte` | number |
| `SByte` | `SByteGraphType` | `sbyte` | number |
| `BigInt` | `BigIntGraphType` | `BigInteger` | number |

Lists of data are also supported with any Scalar or Object types.

| GraphQL    | GraphQL.NET                        | .NET           |
| -----------|-------------------------------------|----------------|
| `[String]` | `ListGraphType<StringGraphType>`    | `List<string>` |
| `[Boolean]` | `ListGraphType<BooleanGraphType>`    | `List<bool>` |

## Objects

Objects are composed of scalar types and other objects.

**GraphQL**

```graphql
type Droid {
  name: String
  appearsIn: [Episode]
}
```

**GraphQL.NET**

```csharp
public class DroidType : ObjectGraphType<Droid>
{
    public DroidType()
    {
        Name = "Droid";
        Description = "A mechanical creature in the Star Wars universe.";
        Field(d => d.Name, nullable: true).Description("The name of the droid.");
        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
    }
}
```

**.NET**

```csharp
public class Droid
{
  public string Name { get; set; }
  public List<Episode> AppearsIn { get; set; }
}
```

## Enumerations

Enumerations, or enums, define a finite set of discrete values. Like scalars, they represent a leaf in the query.

**GraphQL**

This enum defines the first three Star Wars films using GraphQL schema language:

```graphql
enum Episode {
  NEWHOPE
  EMPIRE
  JEDI
}
```

**.NET**

Consider the equivalent `enum` in .NET:

```csharp
public enum Episodes
{
    NEWHOPE = 4,
    EMPIRE = 5,
    JEDI = 6
}
```

Compare the two implementations. GraphQL does not specify backing values for members of its enums. The name of each member _is_ the value.

**GraphQL.NET**

GraphQL.NET provides two methods of defining GraphQL enums.

You can use `EnumerationGraphType<TEnum>` to automatically generate values by providing a .NET `enum` for `TEnum`. The `Name` will default to the .NET Type name, which you can override in the constructor. The `Description` will default to any `System.ComponentModel.DescriptionAttribute` applied to the enum type. The `DeprecationReason` will default to any `System.ObsoleteAttribute` applied to the enum type. By default, the name of each enum member will be converted to CONSTANT_CASE. Override `ChangeEnumCase` to change this behavior. Apply a `DescriptionAttribute` to an enum member to set the GraphQL `Description`. Apply an `ObsoleteAttribute` to an enum member to set the GraphQL `DeprecationReason`.

```csharp
[Description("The Star Wars movies.")]
[Obsolete("Optional. Sets the GraphQL DeprecationReason for the whole enum.")]
public enum Episodes
{
    [Description("Episode 1: The Phantom Menace")]
    [Obsolete("Optional. Sets the GraphQL DeprecationReason for this member.")]
    PHANTOMMENACE = 1,

    [Description("Episode 4: A New Hope")]
    NEWHOPE  = 4,

    [Description("Episode 5: The Empire Strikes Back")]
    EMPIRE  = 5,

    [Description("Episode 6: Return of the Jedi")]
    JEDI  = 6
}

public class EpisodeEnum : EnumerationGraphType<Episodes>
{
}
```

You can also manually create the `EnumerationGraphType`. Advantages of this method:

- The GraphQL enum need not map to a specific .NET `enum`. You could, for instance, build the enum from one of the alternate methods of defining discrete sets of values in .NET, such as classes of constants or static properties.
- You can manually add descriptions and deprecation reasons. This may be useful if you do not control the source code for the enum.
- Backing enum values may be of any type, primitive or not.

```csharp
public class EpisodeEnum : EnumerationGraphType
{
    public EpisodeEnum()
    {
        Name = "Episode";
        Description = "One of the films in the Star Wars Trilogy.";
        AddValue("NEWHOPE", "Released in 1977.", 4);
        AddValue("EMPIRE", "Released in 1980.", 5);
        AddValue("JEDI", "Released in 1983.", 6);
    }
}
```

Note that although GraphQL has no use for backing values for enum members, GraphQL.NET uses them anyway. This allows for a more natural mapping to .NET `enum`s or other collections of constants, and avoids coupling your business logic to GraphQL semantics. The backing values are strictly for use on the back end - the client will never see them.

**Resolving Enumerations**

Fields typed as enumerations are resolved by returning either the name or backing value of one of the enum members. Lists of enumerations are resolved by returning collections of enum members. In the below examples, notice the identical implementations of the `appearsIn` field for both human graph types. In both implementations, the client receives the GraphQL enum member names in response to queries on the `appearsIn` field.

If the field resolves a value which cannot be mapped to one of the enum's legal values, GraphQL.NET will return `null` to the client in the data for the field.

```csharp
public class HumanString
{
    //i.e. "NEWHOPE", "EMPIRE", "JEDI"
    public string[] AppearsIn { get; set; }
}

public class HumanStringType: ObjectGraphType<HumanString>
{
    public HumanStringType()
    {
        Name = "HumanString";
        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
    }
}

public class HumanInt
{
    //i.e. 4, 5, 6
    public int[] AppearsIn { get; set; }
}

public class HumanIntType: ObjectGraphType<HumanInt>
{
    public HumanIntType()
    {
        Name = "HumanInt";
        Field<ListGraphType<EpisodeEnum>>("appearsIn", "Which movie they appear in.");
    }
}
```

**Enumeration Arguments**

Enumerations can be used as arguments in GraphQL queries. Consider a query which gets the humans appearing in a specific film:

```graphql
query HumansAppearingIn($episode: Episode!){
    humans(appearsIn: $episode){
        id
        name
        appearsIn
    }
}

# example query variables:
# {
#   "episode":"NEWHOPE"
# }
```

When GraphQL.NET receives an enum member name as a query argument, the queried field's `ResolveFieldContext` stores the backing value associated with the enum member name in its arguments list. The GraphQL.NET query type which handles the example query may be implemented as:

```csharp
    public class StarWarsQuery : ObjectGraphType<object>
    {
        public StarWarsQuery()
        {
            Name = "Query";

            Field<ListGraphType<HumanType>>(
                "humans",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<EpisodeEnum>>
                        { 
                            Name = "appearsIn", 
                            Description = "An episode the human appears in." 
                        }
                ),
                resolve: context => 
                {
                    // episode = 4
                    var episode = context.GetArgument<int>("appearsIn");

                    // Alternatively, get the argument as an enum. episodeFromEnum = Episodes.NEWHOPE
                    var episodeFromEnum = context.GetArgument<Episodes>("appearsIn");

                    // full implementation would access data store to get humans by episode.
                    return default(Human);
                }
            );
        }
    }
```
