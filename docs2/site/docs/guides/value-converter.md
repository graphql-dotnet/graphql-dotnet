# Value Converter

The `ValueConverter` class provides value conversions between objects of different types. It is used throughout GraphQL.NET
for coercing argument values, parsing scalars, and converting list types. Conversions are registered in a static thread-safe
dictionary and are shared across all schemas in the application.

## Overview

When you call `GetArgument<T>()` on a resolver context, the argument value must be coerced to the requested type.
If the value is already of the correct type, no conversion is needed. Otherwise, `ValueConverter` is consulted to
find a registered conversion delegate. This same mechanism is used internally by scalars and the `ToObject` method.

```csharp
// Example: retrieving an argument that needs conversion
public class MyQuery : ObjectGraphType
{
    public MyQuery()
    {
        Field<StringGraphType>("user")
            .Argument<IdGraphType>("id")
            .Resolve(context =>
            {
                // IdGraphType parses to string, but we want an int
                int userId = context.GetArgument<int>("id");
                return GetUser(userId);
            });
    }
}
```

## Registering Custom Conversions

### Basic Type Conversion

Use `Register<TSource, TTarget>` to register a conversion from one type to another:

```csharp
// Register a conversion from Vector3 to string
ValueConverter.Register<Vector3, string>(v => $"{v.X},{v.Y},{v.Z}");

// Register a conversion from string to Vector3
ValueConverter.Register<string, Vector3>(s =>
{
    var parts = s.Split(',');
    return new Vector3(
        float.Parse(parts[0]),
        float.Parse(parts[1]),
        float.Parse(parts[2])
    );
});
```

Since registrations are static and thread-safe, they should be done once at application startup.
A common pattern is to register conversions in a static constructor:

```csharp
public class MySchema : Schema
{
    static MySchema()
    {
        // Register conversions once for the entire application
        ValueConverter.Register<Vector3, string>(v => $"{v.X},{v.Y},{v.Z}");
        ValueConverter.Register<string, Vector3>(ParseVector3);
    }

    public MySchema(IServiceProvider provider) : base(provider)
    {
        Query = provider.GetRequiredService<MyQuery>();
    }
}
```

### Dictionary to Object Conversion

When input object types are passed as arguments, they arrive as `IDictionary<string, object>`. You can register
a custom converter to transform these dictionaries into strongly-typed objects:

```csharp
public class CreateUserInput
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

// Register the conversion
ValueConverter.Register<CreateUserInput>(dict => new CreateUserInput
{
    Name = (string)dict["name"],
    Email = (string)dict["email"],
    Age = dict.TryGetValue("age", out var age) ? Convert.ToInt32(age) : 0
});
```

## Built-in Conversions

GraphQL.NET registers many conversions by default. Here are the main categories:

### String to Numeric Types

Strings can be converted to all standard numeric types using invariant culture parsing:

| Target Type | Example Input | Example Output |
|-------------|---------------|----------------|
| `int` | `"42"` | `42` |
| `long` | `"9223372036854775807"` | `9223372036854775807L` |
| `float` | `"3.14"` | `3.14f` |
| `double` | `"3.14159265359"` | `3.14159265359` |
| `decimal` | `"123.45"` | `123.45m` |
| `BigInteger` | `"99999999999999999999"` | `BigInteger` value |

### String to Other Types

| Target Type | Example Input |
|-------------|---------------|
| `bool` | `"true"`, `"1"`, `"0"` |
| `Guid` | `"550e8400-e29b-41d4-a716-446655440000"` |
| `DateTime` | `"2024-01-15T10:30:00Z"` |
| `DateTimeOffset` | `"2024-01-15T10:30:00+05:00"` |
| `Uri` | `"https://example.com"` |
| `byte[]` | Base64 encoded string |
| `DateOnly` (.NET 6+) | `"2024-01-15"` |
| `TimeOnly` (.NET 6+) | `"10:30:00"` |

### Numeric Type Conversions

Conversions between numeric types are registered with overflow checking:

```csharp
// These conversions use checked arithmetic
int intValue = ValueConverter.ConvertTo<int>(42L);      // long to int
long longValue = ValueConverter.ConvertTo<long>(42);    // int to long
double doubleValue = ValueConverter.ConvertTo<double>(42); // int to double
```

## List Converter System

GraphQL.NET v8 introduced a comprehensive list converter system for handling conversions to collection types.
This is particularly important for input object lists and for AOT (Ahead-of-Time) compilation scenarios.

### Understanding List Conversion

When an argument contains a list, GraphQL.NET needs to convert it to the target collection type.
The list converter system handles this through two main interfaces:

- `IListConverterFactory` - Creates converters for a family of types (e.g., all `List<T>`)
- `IListConverter` - Converts an `object[]` to a specific list instance

### Default List Type Registrations

These list types are supported out of the box:

| Type | Returned Instance |
|------|-------------------|
| `T[]` | Array of T |
| `IEnumerable<T>` | Array |
| `IList<T>` | Array |
| `ICollection<T>` | Array |
| `IReadOnlyList<T>` | Array |
| `IReadOnlyCollection<T>` | Array |
| `List<T>` | `List<T>` |
| `ISet<T>` | `HashSet<T>` |
| `HashSet<T>` | `HashSet<T>` |
| `IReadOnlySet<T>` (.NET 5+) | `HashSet<T>` |

### Registering Custom List Types

#### Using RegisterListConverterFactory

For open generic types like `ImmutableList<>`, create a factory class:

```csharp
using System.Collections.Immutable;
using GraphQL.Conversion;

public class ImmutableListConverterFactory : ListConverterFactoryBase
{
    public static readonly ImmutableListConverterFactory Instance = new();

    public override Func<object?[], object> Create<T>()
        => list => ImmutableList.CreateRange(list.Cast<T>());
}

// Register for both interface and concrete type
ValueConverter.RegisterListConverterFactory(
    typeof(IImmutableList<>),
    ImmutableListConverterFactory.Instance);

ValueConverter.RegisterListConverterFactory(
    typeof(ImmutableList<>),
    ImmutableListConverterFactory.Instance);
```

You can also use a simpler registration when mapping an interface to a concrete type:

```csharp
// Map IMyList<T> to use MyList<T> implementation
ValueConverter.RegisterListConverterFactory(typeof(IMyList<>), typeof(MyList<>));
```

The implementation type must have either:
- A public constructor accepting `IEnumerable<T>`, or
- A parameterless constructor and a public `Add(T)` method

#### Using RegisterListConverter for Specific Types

For AOT scenarios or when you need precise control over a specific type:

```csharp
// Register a converter for HashSet<string> specifically
ValueConverter.RegisterListConverter<HashSet<string>, string>(
    values => new HashSet<string>(values));

// Register for ImmutableList<int>
ValueConverter.RegisterListConverter<ImmutableList<int>, int>(
    values => ImmutableList.CreateRange(values));
```

### DefaultListConverterFactory

For unregistered types, GraphQL.NET uses `DefaultListConverterFactory` which attempts to create
list instances via reflection. You can customize or disable this:

```csharp
// Use custom fallback factory
ValueConverter.DefaultListConverterFactory = new MyCustomFactory();

// Or disable fallback (throws exception for unknown types)
ValueConverter.DefaultListConverterFactory = null;
```

### AOT Compilation Considerations

When compiling for AOT (Native AOT, iOS, etc.), some reflection-based operations are not available.
GraphQL.NET handles this by:

1. Using optimized code paths that don't require `MakeGenericMethod`
2. Providing `RegisterListConverter<TListType, TElementType>` for explicit type registration

For AOT applications, pre-register all list types you'll use:

```csharp
// Pre-register specific list types for AOT
ValueConverter.RegisterListConverter<List<int>, int>(v => v.ToList());
ValueConverter.RegisterListConverter<List<string>, string>(v => v.ToList());
ValueConverter.RegisterListConverter<HashSet<int>, int>(v => new HashSet<int>(v));
```

## API Reference

### Core Methods

| Method | Description |
|--------|-------------|
| `Register<TSource, TTarget>(Func<TSource, TTarget>)` | Registers a conversion delegate |
| `Register<TTarget>(Func<IDictionary<string, object>, TTarget>)` | Registers dictionary-to-object conversion |
| `Register(Type, Type, Func<object, object>)` | Non-generic registration |
| `ConvertTo<T>(object)` | Converts a value to type T |
| `ConvertTo(object, Type)` | Converts a value to the specified type |
| `GetConversion(Type, Type)` | Returns the registered conversion delegate, if any |

### List Converter Methods

| Method | Description |
|--------|-------------|
| `RegisterListConverterFactory(Type, IListConverterFactory)` | Registers a factory for a list type |
| `RegisterListConverterFactory(Type, Type)` | Maps an interface type to an implementation type |
| `RegisterListConverter<TListType, TElementType>(Func<IEnumerable<TElementType>, TListType>)` | Registers a specific typed list converter |
| `GetListConverterFactory(Type)` | Returns the factory for a list type |
| `GetListConverter(Type)` | Returns a cached converter for a specific list type |

## Best Practices

1. **Register early**: Register all custom conversions at application startup, before any schema is built.

2. **Use static constructors**: Place registrations in a static constructor to ensure they run once.

3. **Handle nulls**: Your conversion delegates should handle null values appropriately.

4. **Use checked arithmetic**: For numeric conversions, use `checked` to catch overflow errors:
   ```csharp
   ValueConverter.Register<long, int>(value => checked((int)value));
   ```

5. **Consider performance**: Conversion delegates are called frequently. Keep them simple and fast.

6. **AOT compatibility**: If targeting AOT, explicitly register all list types you'll use.

## See Also

- [Custom Scalars](../getting-started/custom-scalars) - Using ValueConverter with custom scalar types
- [Arguments](../getting-started/arguments) - Working with GraphQL arguments
- [Schema Types](../getting-started/schema-types) - Understanding GraphQL types in .NET
