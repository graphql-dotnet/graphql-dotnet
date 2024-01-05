# Migrating from v7.x to v8.x

See [issues](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=milestone%3A8.0+is%3Aissue+is%3Aclosed) and
[pull requests](https://github.com/graphql-dotnet/graphql-dotnet/pulls?q=is%3Apr+milestone%3A8.0+is%3Aclosed) done in v8.

## New Features

### 1. `IMetadataReader` and `IMetadataWriter` interfaces added

This makes it convenient to add extension methods to graph types or fields that can be used to read or write metadata
such as authentication information. Methods for `IMetadataWriter` types will appear on both field builders and graph/field
types, while methods for `IMetadataReader` types will only appear on graph and field types. You can also access the
`IMetadataReader` reader from the `IMetadataWriter.MetadataReader` property. Here is an example:

```csharp
public static TMetadataBuilder RequireAdmin<TMetadataBuilder>(this TMetadataBuilder builder)
    where TMetadataBuilder : IMetadataWriter
{
    if (builder.MetadataReader.GetRoles?.Contains("Guests"))
        throw new InvalidOperationException("Cannot require admin and guest access at the same time.");
    return builder.AuthorizeWithRoles("Administrators");
}
```

Both interfaces extend `IProvideMetadata` with read/write access to the metadata contained within the graph or field type.
Be sure not to write metadata during the execution of a query, as the same graph/field type instance may be used for
multiple queries and you would run into concurrency issues.

### 2. Built-in scalars may be overridden via DI registrations

For GraphQL.NET built-in scalars (such as `IntGraphType` or `GuidGraphType`), a dervied class may be registered
within the DI engine to facilitate replacement of the graph type throughout the schema versus calling `RegisterType`.

```csharp
services.AddSingleton<BooleanGraphType, MyBooleanGraphType>();
```

See https://graphql-dotnet.github.io/docs/getting-started/custom-scalars/#3-register-the-custom-scalar-within-your-schema
for more details.

### 3. Added `ComplexScalarGraphType`

This new scalar can be used to send or receive arbitrary objects or values to or from the server. It is functionally
equivalent to the `AnyGraphType` used for GraphQL Federation, but defaults to the name of `Complex` rather than `_Any`.

### 4. `Parser` delegates added to input field and argument definitions

This allows for custom parsing of input values. The `Parser` delegate is used to convert the input value
to the expected type, and can be set via the `ParseValue` method on the `FieldBuilder` or `QueryArgument`.

The auto-registering graph types will automatically configure the `Parser` delegate appropriately, and the
`Field(x => x.Property)` and `Field("FieldName", x => x.Property)` syntax will as well.

The most common use case for this is when using the ID graph type (passed via a string) for a numeric or GUID identifier.
For example, consider the following code:

```csharp
// for input object graph type
Field("Id", x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));

class MyInputObject
{
    public int Id { get; set; }
}
```

This will now cause an error when the client sends a string value for the Id field that cannot be coerced to
an `int` during the validation stage, rather than during the execution stage. Supplying an invalid value will
produce a response similar to the following:

```json
{
  "errors": [
    {
      "message": "Invalid value for argument 'id' of field 'testMe'. The input string 'abc' was not in a correct format.",
      "locations": [
        {
          "line": 1,
          "column": 14
        }
      ],
      "extensions": {
        "code": "INVALID_VALUE",
        "codes": [
          "INVALID_VALUE",
          "FORMAT"
        ],
        "number": "5.6"
      }
    }
  ]
}
```

This now is a validation error and not passed to the unhandled exception handler. Previously, this would have been
considered a server exception and processed by the unhandled exception handler, returning an error similar to the
following:

```json
{
  "errors": [
    {
      "message": "Error trying to resolve field 'testMe'.",
      "locations": [
        {
          "line": 1,
          "column": 3
        }
      ],
      "path": [
        "testMe"
      ],
      "extensions": {
        "code": "FORMAT",
        "codes": [
          "FORMAT"
        ]
      }
    }
  ],
  "data": null
}
```

You can also define a custom parser when appropriate to convert an input value to the expected type.
This is typically unnecessary when using the `Field(x => x.Property)` syntax, but when matching via
property name, it may be desired to define a custom parser. For example:

```csharp
// for input object graph type
Field<StringGraphType>("website") // match by property name, perhaps for a constructor argument
    .ParseValue(value => new Uri((string)value));

class MyInputObject
{
    public Uri? Website { get; set; }
}
```

Without adding a parser the coercion will occur within the resolver during `GetArgument<Uri>("abc")`
as occured in previous versions of GraphQL.NET. This will result in a server exception being thrown
and processed by the unhandled exception handler if the value cannot be coerced to a `Uri`. Note that
the parser function need not check for null values.

### 5. `Validator` delegates added to input field and argument definitions

This allows for custom validation of input values. It can be used to easily validate input values
such as email addresses, phone numbers, or to validate a value is within a specific range. The
`Validator` delegate is used to validate the input value, and can be set via the `Validate` method
on the `FieldBuilder` or `QueryArgument`. Here are some examples:

```csharp
// for an input object graph type
Field(x => x.FirstName)
    .Validate(value =>
    {
        if (((string)value).Length >= 10)
            throw new ArgumentException("Length must be less than 10 characters.");
    });
Field(x => x.Age)
    .Validate(value =>
    {
        if ((int)value < 18)
            throw new ArgumentException("Age must be 18 or older.");
    });
Field(x => x.Password)
    .Validate(value =>
    {
        VerifyPasswordComplexity((string)value);
    });
```

The `Validator` delegate is called during the validation stage, prior to execution of the request.
Null values are not passed to the validation function. Supplying an invalid value will produce
a response similar to the following:

```json
{
  "errors": [
    {
      "message": "Invalid value for argument 'firstName' of field 'testMe'. Length must be less than 10 characters.",
      "locations": [
        {
          "line": 1,
          "column": 14
        }
      ],
      "extensions": {
        "code": "INVALID_VALUE",
        "codes": [
          "INVALID_VALUE",
          "ARGUMENT"
        ],
        "number": "5.6"
      }
    }
  ]
}
```

For type-first schemas, you may define your own attributes to perform validation, either on input
fields or on output field arguments. For example:

```csharp
// for AutoRegisteringObjectGraphType<MyClass>

public class MyClass
{
    public static string TestMe([MyMaxLength(5)] string value) => value;
}

private class MyMaxLength : GraphQLAttribute
{
    private readonly int _maxLength;
    public MyMaxLength(int maxLength)
    {
        _maxLength = maxLength;
    }

    public override void Modify(ArgumentInformation argumentInformation)
    {
        if (argumentInformation.TypeInformation.Type != typeof(string))
        {
            throw new InvalidOperationException("MyMaxLength can only be used on string arguments.");
        }
    }

    public override void Modify(QueryArgument queryArgument)
    {
        queryArgument.Validate(value =>
        {
            if (((string)value).Length > _maxLength)
            {
                throw new ArgumentException($"Value is too long. Max length is {_maxLength}.");
            }
        });
    }
}
```

Similar to the `Parser` delegate, the `Validator` delegate is called during the validation stage,
and will not unnecessarily trigger the unhandled exception handler due to client input errors.

At this time GraphQL.NET does not directly support the `MaxLength` and similar attributes from
`System.ComponentModel.DataAnnotations`, but this may be added in a future version. You can
implement your own attributes as shown above, or call the `Validate` method to set a validation
function.

### 6. `@pattern` custom directive added for validating input values against a regular expression pattern

This directive allows for specifying a regular expression pattern to validate the input value.
It can also be used as sample code for designing new custom directives, and is now the preferred
design over the older `InputFieldsAndArgumentsOfCorrectLength` validation rule.
This directive is not enabled by default, and must be added to the schema as follows:

```csharp
services.AddGraphQL(b => b
    .AddSchema<MyQuery>()
    .ConfigureSchema(s =>
    {
        s.Directives.Register(new PatternMatchingDirective());
        s.RegisterVisitor(new PatternMatchingVisitor());
    }));
```

You can then apply the directive to any input field or argument as follows:

```csharp
Field(x => x.FirstName)
    .ApplyDirective("pattern", "regex", "[A-Z]+"); // uppercase only
```

### 7. DirectiveAttribute added to support applying directives to type-first graph types and fields

For example:

```csharp
private class Query
{
    public static string Hello(
        [Directive("pattern", "regex", "[A-Z]+")] // uppercase only
        string arg)
        => arg;
}
```

## Breaking Changes

### 1. Query type is required

Pursuant to the GraphQL specification, a query type is required for any schema.
This is enforced during schema validation but may be bypassed as follows:

```csharp
GlobalSwitches.RequireRootQueryType = false;
```

Future versions of GraphQL.NET will not contain this property and each schema will always be required to have a root Query type to comply with the GraphQL specification.

### 2. Use `ApplyDirective` instead of `Directive` on field builders

The `Directive` method on field builders has been renamed to `ApplyDirective` to better fit with
other field builder extension methods.

### 3. Use `WithComplexityImpact` instead of `ComplexityImpact` on field builders

The `ComplexityImpact` method on field builders has been renamed to `WithComplexityImpact` to better fit with
other field builder extension methods.

### 4. Relay types must be registered within the DI provider

Previuosly the Relay graph types were instantiated directly by the `SchemaTypes` class. This has been changed so that
the types are now pulled from the DI container. No changes are required if you are using the provided DI builder methods,
as they automatically register the relay types. Otherwise, you will need to manually register the Relay graph types.

```csharp
// v7 and prior -- builder methods -- NO CHANGES REQUIRED
services.AddGraphQL(b => {
  b.AddSchema<StarWarsSchema>();
});

// v7 and prior -- manual registration
services.AddSingleton<StarWarsSchema>(); // and other types

// v8
services.AddSingleton<PageInfoType>();
services.AddSingleton(typeof(EdgeType<>);
services.AddSingleton(typeof(ConnectionType<>);
services.AddSingleton(typeof(ConnectionType<,>);
```

### 5. Duplicate GraphQL configuration calls with the same `Use` command is ignored

Specifically, this relates to the following methods:

- `UseMemoryCache()`
- `UseAutomaticPersistedQueries()`
- `UseConfiguration<T>()` with the same `T` type

This change was made to prevent duplicate registrations of the same service within the DI container.

### 6. `ObjectExtensions.ToObject` changes (impacts `InputObjectGraphType`)

- `ObjectExtensions.ToObject<T>` was removed; it was only used by internal tests.
- `ObjectExtensions.ToObject` requires input object graph type for conversion.
- Only public constructors are eligible candidates while selecting a constructor.
- Constructor is selected based on the following rules:
  - If only a single public constructor is available, it is used.
  - If a public constructor is marked with `[GraphQLConstructor]`, it is used.
  - Otherwise the public parameterless constructor is used if available.
  - Otherwise an exception is thrown during deserialization.
- Only public properties are eligible candidates when matching a property.
- Any init-only or required properties not provided in the dictionary are set to their default values.
- Only public writable fields are eligible candidates when matching a field.

The changes above allow for matching behavior with source-generated or dynamically-compiled functions.

### 7. `AutoRegisteringInputObjectGraphType` changes

- See above changes to `ObjectExtensions.ToObject` for deserialization notes.
- Registers read-only properties when the property name matches the name of a parameter in the
  chosen constructor. Comparison is case-insensitive, matching `ToObject` behavior.
  Does not register constructor parameters that are not read-only properties.
  Any attributes such as `[Id]` must be applied to the property, not the constructor parameter.

### 8. Default naming of generic graph types has changed

The default graph name of generic types has changed to include the generic type name.
This should reduce naming conflicts when generics are in use. To consolidate behavior
across different code paths, both `Type` and `GraphType` are stripped from the end
of the class name. See below examples:

| Graph type class name | Old graph type name | New graph type name |
|------------------|---------------------|---------------------|
| `PersonType` | `PersonType` | `Person` |
| `PersonGraphType` | `Person` | `Person` |
| `AutoRegisteringObjectGraphType<SearchResults<Person>>` | `SearchResults` | `PersonSearchResults` |
| `LoggerGraphType<Person, string>` | `Logger` | `PersonStringLogger` |
| `InputObjectGraphType<Person>` | `InputObject_1` | `PersonInputObject` |

To revert to the prior behavior, set the following global switch prior to creating your schema classes:

```csharp
using GraphQL;

GlobalSwitches.UseLegacyTypeNaming = true;
```

As usual, you are encouraged to set the name in the constructor of your class, or
immediately after construction, or for auto-registering types, via an attribute.
You can also set global attributes that will be applied to all auto-registering types
if you wish to define your own naming logic.

The `UseLegacyTypeNaming` option is deprecated and will be removed in GraphQL.NET v9.

### 9. DataLoader extension methods have been moved to the GraphQL namespace

This change simplifies using extension methods for the data loaders. You may need to
remove the `using GraphQL.DataLoader;` statement from your code to resolve any
compiler warnings, and/or add `using GraphQL;`.

### 10. The SchemaPrinter has been deprecated

Please see the v7 migration document regarding the new `schema.ToAST()` and
`schema.Print()` methods available for printing the schema (available since 7.6).

For federated schemas, the `ServiceGraphType`'s `sdl` field will now use the
new implementation to print the schema. Please raise an issue if this causes
a problem for your federated schema.
