# ValueConverter

GraphQL.NET's **ValueConverter** API allows you to transform values between your .NET types and the GraphQL type system. It normalizes values when executing queries and mutations so that your field resolvers receive CLR types and clients receive serialized GraphQL values.

## Why use a ValueConverter?

- **Normalizes incoming variables**: converts JSON/GraphQL literals into .NET types.
- **Serializes return values**: converts CLR objects back into GraphQL‑friendly representations.
- **Extensible**: you can register your own converters for custom types or override existing behaviour.

Most primitive types (e.g. `int`, `float`, `string`, `enum`) are handled out of the box. When you have special types (like value objects or structs) you can implement a converter to control how they are read and written.

## Creating a custom ValueConverter

Implement `IValueConverter` to describe how to convert your type. The `Convert` method receives the raw value and a context describing the target type.

```csharp
public class MyValueConverter : IValueConverter
{
    public object? Convert(object? value, IValueConverterContext context)
    {
        // add your conversion logic here
        if (value is string s)
        {
            // Example: convert a comma‑separated string into a custom type
            return MyType.Parse(s);
        }
        return value;
    }
}
```

Register your converter with the dependency injection container or schema configuration so GraphQL.NET can discover it:

```csharp
services.AddSingleton<IValueConverter, MyValueConverter>();
```

## List converters

When working with lists, GraphQL.NET uses the `IListConverterFactory` interface and the `ListConverterFactoryBase` base class to build converters that handle collections. These types create element converters and manage nullable lists. You typically don’t need to implement these yourself unless you are providing custom list semantics.

## Related migration notes

The `ValueConverter` system was enhanced alongside the addition of `IListConverterFactory` and `ListConverterFactoryBase`. For AOT‑compilation scenarios and migration notes, please see:

- [AOT compatibility for IListConverter](https://github.com/graphql-dotnet/graphql-dotnet/issues/3932)
- [Add IListConverterFactory & ListConverterFactoryBase](https://github.com/graphql-dotnet/graphql-dotnet/issues/3931)

For a deeper dive, check out the source code and existing documentation on custom scalars and converters. Feel free to contribute examples or improvements to this page!
