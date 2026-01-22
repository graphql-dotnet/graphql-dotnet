# ValueConverter

The `ValueConverter` is a core component in GraphQL.NET that handles type conversions between different .NET types. It is primarily used when retrieving argument values with `GetArgument<T>` and when converting complex input objects with `ToObject`. This page describes the purpose of the value converter, how to use it, and how to extend it for custom conversions.

## Overview

The `ValueConverter` manages conversions between types in several scenarios:

1. **Argument Value Retrieval**: When you call `context.GetArgument<T>("argName")`, the value converter coerces the argument value to the requested type `T` if necessary.
2. **Complex Input Object Deserialization**: When an input graph type receives a dictionary of values (from variables or arguments), the value converter converts each property to the appropriate .NET type.
3. **Scalar Type Processing**: Custom scalar types may leverage the value converter to handle type conversions in their `ParseValue` methods.
4. **List Type Conversions**: The value converter can convert arrays to specific list types like `List<T>`, `HashSet<T>`, or `ImmutableList<T>`.

The value converter contains a registry of conversion functions that map from one type to another. GraphQL.NET includes built-in conversions for common types, and you can register your own custom conversions.

## How Value Conversion Works

When `GetArgument<T>` is called, GraphQL.NET follows this process:

1. Retrieve the argument value from the arguments dictionary
2. If the value is already of type `T`, return it as-is
3. If the value is `null`, return `null` (or default value for value types)
4. Look up a registered conversion function from the value's type to type `T`
5. If a conversion exists, apply it and return the result
6. If no conversion exists, attempt to handle special cases:
   - Enumerations: Parse string or numeric values to enum
   - Complex objects: Use `ToObject` to deserialize dictionaries into typed objects
   - Lists: Use list converters to create the appropriate collection type
7. If all else fails, throw an exception

## Built-in Conversions

GraphQL.NET registers many built-in conversions automatically. These include:

### String Conversions
Conversions from `string` to:
- All numeric types (`sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `BigInteger`, `float`, `double`, `decimal`)
- `bool` (supports "0"/"1" and "true"/"false")
- `DateTime` and `DateTimeOffset`
- `DateOnly` and `TimeOnly` (.NET 6+)
- `Guid`
- `Uri`
- `byte[]` (Base64 decoding)

### Numeric Conversions
- Between integer types (with overflow checking)
- Between floating-point types
- Widening conversions (e.g., `int` to `long`, `float` to `double`)
- Narrowing conversions with `checked` arithmetic (e.g., `long` to `int`)

### Date/Time Conversions
- `DateTime` ↔ `DateTimeOffset`
- `TimeSpan` ↔ `long` (total seconds)
- `int`/`long` to `TimeSpan` (seconds)

### Other Conversions
- `int` to `bool` (0 = false, non-zero = true)

See the [source code](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Conversion/ValueConverterBase.cs) for the complete list of built-in conversions.

## Registering Custom Conversions

You can register your own type conversions globally by calling the static `Register` method on `ValueConverter`. This is typically done once during application startup, such as in a static constructor of your schema.

### Example: Converting a Custom Type to String

Suppose you have a custom `Vector3` struct and want to support converting it to a string:

```csharp
public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class MySchema : Schema
{
    static MySchema()
    {
        // Register conversion from Vector3 to string
        ValueConverter.Register<Vector3, string>(v => $"{v.X},{v.Y},{v.Z}");
    }
    
    public MySchema()
    {
        Query = new MyQuery();
    }
}
```

Now when you call `context.GetArgument<string>("vector")` where the argument is a `Vector3`, the conversion will be applied automatically.

### Example: Converting Between Custom Types

You can also register conversions between your own custom types:

```csharp
public class UserId
{
    public int Value { get; set; }
}

public class UserIdInput
{
    public int Id { get; set; }
}

static MySchema()
{
    // Register conversion from UserIdInput to UserId
    ValueConverter.Register<UserIdInput, UserId>(input => new UserId { Value = input.Id });
}
```

### Registering Converters Per Schema Instance

While the static `Register` method is convenient for global conversions, you can also register conversions for a specific schema instance. Each schema has its own `ValueConverter` instance accessible via the `Converter` property:

```csharp
public class MySchema : Schema
{
    public MySchema()
    {
        Query = new MyQuery();
        
        // Register a schema-specific conversion
        this.Converter.Register<MyCustomType, string>(value => value.ToString());
    }
}
```

Schema-specific converters are useful when different schemas need different conversion behaviors.

## List Converters

The value converter includes support for converting arrays to specific list types. This is useful when you want to receive arguments or input fields as specific collection types like `HashSet<T>`, `ImmutableList<T>`, or custom collection types.

### Built-in List Converters

GraphQL.NET automatically registers converters for common list types:

- **Arrays**: `ICollection`, `IEnumerable`, `IList`, `IList<T>`, `IEnumerable<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` → array (`T[]`)
- **List<T>**: `List<T>` → `List<T>` (when not running under AOT)
- **HashSet<T>**: `ISet<T>`, `HashSet<T>` → `HashSet<T>`
- **IReadOnlySet<T>**: `IReadOnlySet<T>` → `HashSet<T>` (.NET 5+)
- **Immutable Collections**: `ImmutableList<T>`, `IImmutableList<T>`, `ImmutableHashSet<T>`, `IImmutableSet<T>`, `ImmutableArray<T>` (.NET Core 1.0+)

### Using List Converters in Arguments

When you define a field argument with a list type, the value converter automatically handles the conversion:

```csharp
Field<StringGraphType>("processItems")
    .Argument<ListGraphType<IntGraphType>>("items")
    .Resolve(context =>
    {
        // Automatically converts to HashSet<int>
        var items = context.GetArgument<HashSet<int>>("items");
        return $"Processing {items.Count} unique items";
    });
```

In this example, even though the GraphQL argument is a list, the value converter transforms it into a `HashSet<int>` automatically, removing any duplicates.

### Registering Custom List Converters

You can register custom list converters for your own collection types. There are two approaches:

#### 1. Using RegisterListConverterFactory with Implementation Type

If you have a custom list type that has a constructor accepting `IEnumerable<T>` or implements `IList`, you can register it with an implementation type:

```csharp
public class MyCustomList<T> : IList<T>
{
    private List<T> _inner = new List<T>();
    
    // IList<T> implementation...
    
    public MyCustomList() { }
    
    public MyCustomList(IEnumerable<T> items)
    {
        _inner.AddRange(items);
    }
}

static MySchema()
{
    // Register the custom list type
    ValueConverter.RegisterListConverterFactory(
        typeof(MyCustomList<>), 
        typeof(MyCustomList<>)
    );
}
```

#### 2. Using IListConverterFactory for Advanced Scenarios

For more control over the conversion process, you can implement `IListConverterFactory`:

```csharp
public class MyListConverterFactory : IListConverterFactory
{
    public IListConverter Create(Type listType)
    {
        // Get the element type from the list type
        var elementType = listType.GetGenericArguments()[0];
        
        // Create a converter that converts object[] to your list type
        Func<object?[], object> converter = items =>
        {
            // Custom conversion logic here
            var list = Activator.CreateInstance(listType);
            foreach (var item in items)
            {
                // Add items to your custom list
            }
            return list;
        };
        
        return new ListConverter(elementType, converter);
    }
}

static MySchema()
{
    ValueConverter.RegisterListConverterFactory(
        typeof(MyCustomList<>), 
        new MyListConverterFactory()
    );
}
```

### ListConverterFactoryBase

For most custom list converters, you can inherit from `ListConverterFactoryBase`, which provides a convenient base implementation:

```csharp
public class MyListConverterFactory : ListConverterFactoryBase
{
    public override Func<object?[], object> Create<T>()
    {
        return items => new MyCustomList<T>(items.Cast<T>());
    }
}
```

`ListConverterFactoryBase` handles the reflection and generic type construction for you. You only need to implement the `Create<T>` method that returns a function to convert an array to your list type.

## AOT Compatibility

When running under Native AOT (Ahead-of-Time compilation), dynamic code generation is not available. GraphQL.NET provides AOT-compatible implementations:

- **ValueConverterAot**: An AOT-compatible value converter that doesn't rely on dynamic code generation
- **CustomListConverterFactory**: Used for custom list types when running under AOT

The `ValueConverter` class automatically detects whether the application is running under AOT and uses appropriate implementations. In AOT scenarios:

- `List<T>` converters use `CustomListConverterFactory` instead of dynamic code
- `HashSet<T>` converters also use `CustomListConverterFactory`
- You should prefer `CustomListConverterFactory` when registering custom list types

For more information about AOT support, see the [migration guide](../migrations/migration9.md#3-graphqlaotserializer-for-native-aot-support).

## ToObject Method

The `ToObject` method on the value converter deserializes complex input objects from dictionaries. This is used internally when processing input graph types but can also be called directly:

```csharp
public class PersonInput
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class MySchema : Schema
{
    public MySchema()
    {
        Field<StringGraphType>("createPerson")
            .Argument<PersonInputType>("input")
            .Resolve(context =>
            {
                // GetArgument uses ToObject internally
                var person = context.GetArgument<PersonInput>("input");
                return $"Created {person.Name}, age {person.Age}";
            });
    }
}
```

The `ToObject` method:
1. Creates an instance of the target type
2. Iterates through each property in the dictionary
3. Converts each value to the property's type using the value converter
4. Sets the property value on the created object

You typically don't need to call `ToObject` directly; `GetArgument<T>` handles it for you. However, you can call it explicitly if needed:

```csharp
var dict = new Dictionary<string, object?>
{
    ["name"] = "John",
    ["age"] = 30
};

var person = schema.Converter.ToObject(dict, typeof(PersonInput), personInputGraphType);
```

## Common Use Cases

### 1. Handling IDs as Integers

GraphQL `ID` types are typically transmitted as strings, but your code may use integers internally:

```csharp
Field<WidgetType>("widget")
    .Argument<IdGraphType>("id")
    .Resolve(context =>
    {
        // Automatically converts string ID to integer
        var id = context.GetArgument<int>("id");
        return _widgetRepository.GetById(id);
    });
```

The built-in string-to-int conversion handles this automatically.

### 2. Date/Time Handling

GraphQL typically transmits dates as ISO 8601 strings. The value converter handles these conversions:

```csharp
Field<StringGraphType>("schedule")
    .Argument<StringGraphType>("startDate")
    .Resolve(context =>
    {
        // Converts ISO 8601 string to DateTime
        var startDate = context.GetArgument<DateTime>("startDate");
        return $"Event starts at {startDate:yyyy-MM-dd}";
    });
```

### 3. Enum Values

The value converter automatically handles enum conversions from strings and integers:

```csharp
public enum Priority { Low, Medium, High }

Field<StringGraphType>("createTask")
    .Argument<EnumerationGraphType<Priority>>("priority")
    .Resolve(context =>
    {
        // Automatically converts from GraphQL enum to .NET enum
        var priority = context.GetArgument<Priority>("priority");
        return $"Task created with {priority} priority";
    });
```

### 4. Nullable Types

The value converter respects nullable value types:

```csharp
Field<StringGraphType>("updateTask")
    .Argument<IntGraphType>("id")
    .Argument<IntGraphType>("priority", nullable: true)
    .Resolve(context =>
    {
        var id = context.GetArgument<int>("id");
        var priority = context.GetArgument<int?>("priority");
        
        if (priority.HasValue)
            return $"Updated task {id} with priority {priority.Value}";
        else
            return $"Updated task {id} without changing priority";
    });
```

## Best Practices

1. **Register conversions early**: Register global conversions in static constructors or during application startup before the schema is initialized.

2. **Use schema-specific converters when appropriate**: If different schemas need different conversion logic, use the schema's `Converter` property instead of static registration.

3. **Avoid circular conversions**: Don't register conversions that could create circular dependencies (e.g., A→B and B→A).

4. **Handle exceptions gracefully**: Conversion functions can throw exceptions. Make sure your conversions handle invalid input appropriately.

5. **Consider AOT compatibility**: If you plan to deploy with Native AOT, test your custom converters in an AOT environment.

6. **Document custom conversions**: If you register custom conversions, document them for maintainability.

## Related Topics

- [Custom Scalars](custom-scalars.md) - Learn how to create custom scalar types that may use the value converter
- [Arguments](arguments.md) - Understand how arguments work with `GetArgument<T>`
- [Migration Guide v9](../migrations/migration9.md) - See changes to value conversion in v9
- [Migration Guide v8](../migrations/migration8.md) - Learn about list converter factories introduced in v8

## See Also

- [ValueConverter source code](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Conversion/ValueConverter.cs)
- [IListConverterFactory source code](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Conversion/IListConverterFactory.cs)
- [ListConverterFactoryBase source code](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Conversion/ListConverterFactoryBase.cs)
