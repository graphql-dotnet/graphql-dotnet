# Schema Types

## Scalars

A GraphQL object type has a name and fields, but at some point those fields have to resolve to some concrete data. That's where the scalar types come in: they represent the leaves of the query.

These are the scalars provided by the GraphQL Specification.

| GraphQL     | GraphQL .NET        | .NET                    |
|-------------|---------------------|-------------------------|
| `String`    | `StringGraphType`   | `string`                |
| `Int`       | `IntGraphType`      | `int` `long`            |
| `Float`     | `FloatGraphType`    | `double`                |
| `Boolean`   | `BooleanGraphType`  | `bool`                  |
| `ID`        | `IdGraphType`       | `int`, `long`, `string` |

> Note that you can use a `Guid` with `ID`.  It will just be serialized to a `string` and should be sent to your GraphQL Server as a `string`.

These are additional scalars provided by this project.

| GraphQL          | GraphQL .NET                    | .NET               |
|------------------|---------------------------------|--------------------|
| `Date`           | `DateGraphType`                 | `DateTime`         |
| `DateTime`       | `DateTimeGraphType`             | `DateTime`         |
| `DateTimeOffset` | `DateTimeOffsetGraphType`       | `DateTimeOffset`   |
| `Seconds`        | `TimeSpanSecondsGraphType`      | `TimeSpan`         |
| `Milliseconds`   | `TimeSpanMillisecondsGraphType` | `TimeSpan`         |
| `Decimal` | `DecimalGraphType` | `decimal` |
| `Uri` | `UriGraphType` | `Uri` |
| `Guid` | `GuidGraphType` | `Guid` |
| `Short` | `ShortGraphType` | `short` |
| `UShort` | `UShortGraphType` | `ushort` |
| `UInt` | `UIntGraphType` | `uint` |
| `ULong` | `ULongGraphType` | `ulong` |
| `Byte` | `ByteGraphType` | `byte` |
| `SByte` | `SByteGraphType` | `sbyte` 

Lists of data are also supported with any Scalar or Object types.

| GraphQL    | GraphQL .NET                        | .NET           |
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

**GraphQL .NET**

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

**GraphQL**

```graphql
enum Episode {
  NEWHOPE
  EMPIRE
  JEDI
}
```

**GraphQL .NET**

You can manually create the `EnumerationGraphType`.

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

Or you can use the generic version passing it a .NET `enum` which will populate values and descriptions (if any defined via `DescriptionAttribute`) for you. The `Name` will default to the .NET Type name, which you can override in the constructor.

```csharp
public class EpisodeEnum : EnumerationGraphType<Episodes>
{
}
```

**.NET**

```csharp
public enum Episodes
{
    NEWHOPE  = 4,
    EMPIRE  = 5,
    JEDI  = 6
}
```
