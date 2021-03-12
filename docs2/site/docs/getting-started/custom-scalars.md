# Custom Scalars

You can extend your schema with your own custom scalars. Conceptually, a scalar must implement the following operations:

- Serialization: Transforms a scalar from its server-side representation to a representation suitable for the client.

- Value Parsing: Transforms a scalar from its client-side representation as a variable to its server-side representation.

- Literal Parsing: Transforms a scalar from its client-side representation as an argument to its server-side representation.

Parsing for arguments and variables are handled separately because while arguments must always be expressed in GraphQL
query syntax, variable format is transport-specific (usually JSON). You can find more information about
these methods [here](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/Types/Scalars/ScalarGraphType.cs).

You may wish to read more about how scalars work at the following links:

- https://www.graphql-scalars.com/scalars-guide/
- https://www.graphql.de/blog/scalars-in-depth/

## Vector3 sample with string parsing and serialization

The following example shows how to create a custom scalar in GraphQL.NET. You will create a 3D Vector which will be exchanged
between server and client as a comma-separated string (ex. "34, 61, 12"). The example assumes the GraphQL schema is implemented
in an ASP.NET Core project using the `Microsoft.Extensions.DependencyInjection` package, though only minor modifications would
be required for other project types.

Assume the following schema

```graphql
scalar Vector3

schema {
    query: {
        getVector: Vector3!
    }
    mutation: {
        addVector(vector3: Vector3!): Vector3
    }
}
```

The goal is to execute mutations with both arguments:

```graphql
mutation {
    addVector(vector3: "23, 43, 66")
}
```

and also variables:

```graphql
mutation AddVector($vector3: Vector3!) {
    addVector(vector3: $vector3)
}

//variables
{
    "vector3": "23, 43, 66"
}
```

Vectors should be returned in the same format:

```json
{
    "data": {
        "getVector": "23, 43, 66"
    }
}
```

### 1. Create the class for the server-side representation.

```csharp
public struct Vector3
{
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }       
}
```

### 2. Create a graph type for the scalar by inheriting `ScalarGraphType`.

```csharp
using GraphQL;
using GraphQL.Types;
using GraphQL.Language.AST;

public class Vector3Type : ScalarGraphType
{
    public Vector3Type()
    {
        Name = "Vector3";
    }

    public override object ParseLiteral(IValue value)
    {
        throw new NotImplementedException();
    }

    public override object ParseValue(object value)
    {
        throw new NotImplementedException();
    }

    public override object Serialize(object value)
    {
        throw new NotImplementedException();
    }
}
```

### 3. Register the graph type with the DI container.

```csharp
// In Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<Vector3Type>();

    // Other schema registrations...
}
```

### 4. Prepare to accept `Vector3` inputs from query arguments. Implement `ScalarGraphType.ParseLiteral`.

Keep in mind that AST parsing may present values as any of the following types:
- `IntValue` - integers that can be represented within an `int`
- `LongValue` - integers that can be represented within a `long`
- `BigIntValue` - integers that can be represented within a `BigInteger`
- `FloatValue` - floating-point values that can be represented within a `double`
- `DecimalValue` - floating-point values that can be represented within a `decimal`
- `StringValue` - string values
- `EnumValue` - enumeration values
- `NullValue` - representing `null` - must be handled by all scalars

If your custom scalar accepts floating-point values, you must be sure to handle all 5 of the
numeric types, since queries like `{ field(arg: 3) }` is parsed as an `IntValue` even though
it is also a valid floating-point number.

In the sample below, only `NullValue` and `StringValue` need to be handled.

For any type that is not handled, or when the value cannot be parsed, you must throw an exception.
`ThrowLiteralConversionError` is provided as a convenient method to facilitate throwing an exception
when the type does not match.

```csharp
//in Vector3Type

public override object ParseLiteral(IValue value)
{
    if (value is NullValue)
        return null;

    if (value is StringValue stringValue)
        return ParseValue(stringValue.Value);

    return ThrowLiteralConversionError(value);
}
```

Once the raw string is extracted from the value node, normal parsing can proceed.

### 5. Prepare to accept `Vector3` inputs from query variables. Implement `ScalarGraphType.ParseValue`.

Similar to `ParseLiteral`, you must keep in mind the expected format of values that are likely
to be presented to this method. For instance, if you are using a JSON deserializer, you may be
presented with values of any of these types (or more, depending on your deserializer configuration):

- `int`
- `long`
- `ulong`
- `BigInteger`
- `double`
- `decimal`
- `string`
- `null` - must be handled by all scalars

On top of that, if you are calling this method from `ParseLiteral`, you must handle the types
passed from it. So if your scalar needs to handle floating-point values, you likely need to handle
`int`, `long`, `ulong`, `BigInteger`, `double` and `decimal` types.

For any type that is not handled, or when the value cannot be parsed, you must throw an exception.
`ThrowValueConversionError` is provided as a convenient method to facilitate throwing an exception
when the type does not match.

In the sample below, only `null` and `string` types need to be handled.

```csharp
// In Vector3Type
public override object ParseValue(object value)
{
    if (value == null)
        return null;

    if (value is string vector3InputString)
    {
        try
        {
            var vector3Parts = vector3InputString.Split(',');
            var x = float.Parse(vector3Parts[0]);
            var y = float.Parse(vector3Parts[1]);
            var z = float.Parse(vector3Parts[2]);
            return new Vector3(x, y, z);
        }
        catch
        {
            throw new FormatException($"Failed to parse {nameof(Vector3)} from input '{vector3InputString}'. Input should be a string of three comma-separated floats in X Y Z order, ex. 1.0,2.0,3.0");
        }
    }

    return ThrowValueConversionError(value);
}
```

### 6. Implement `ScalarGraphType.Serialize` so `Vector3` instances can be sent to the client.
Keep in mind that a `null` value also required to be handled.

It is recommended that the type of data this method returns match the same type as
is handled within `ParseValue`. In this case, serialization of `Vector3` is serialized as
a string, which matches the expected data type within `ParseValue`.

```csharp
// In Vector3Type

public override object Serialize(object value)
{
    if (value == null)
        return null;

    if (value is Vector3 vector3)
    {
        return $"{vector3.X},{vector3.Y},{vector3.Z}";
    }

    return ThrowSerializationError(value);
}
```

### 7. Override `ScalarGraphType.ToAST` if necessary.

The infrastructure converts default field values to AST representations during initialization
in order to verify that the default values are valid within an AST tree. The default implementation
calls `Serialize` to convert the value to its client-side equivalent, then embeds it into an
appropriate AST node based on the data type returned from `Serialize`. It is likely you will
only need to override this method if you are creating a custom scalar that returns enumeration
values, or if you are returning structured data.

In this example, you created a custom scalar. In summary:

- Create a data class for the server-side representation of the scalar
- Implement a `ScalarGraphType` which handles variable parsing, literal parsing, and serialization
- Register the `ScalarGraphType` within the DI container

You can also choose to override `CanParseLiteral`, `CanParseValue` or `IsValidDefault` for
enhanced performance. The default implementations call `ParseLiteral`, `ParseValue` and
`ToAST` respectively, returning `false` if an exception is caught, or `true` otherwise.
If you do choose to implement these methods, note that those methods must not throw an
exception, and that they are not always called when executing a document.

## Vector3 sample with combined string/structured parsing and serialization

Keep in mind that the serialized value returned by custom scalar can by anything that the
environment allows. For example it can be a structured object, rather than a simple value.
This is also true of variables, however literals can only be the represented by the native
scalar types supported by the GraphQL query language -- integers, floating-point values,
booleans and null values. You cannot parse a structured literal object into a scalar value.

So to extend our sample, let's assume that we want the Vector3 scalar to instead accept and
return data in a more structured format, in addition to supporting the string format for literals.

Here is a sample of a variable supporting a more structured format:

```graphql
mutation AddVector($vector3: Vector3!) {
    addVector(vector3: $vector3)
}

//variables
{
    "vector3": {
        "X":"23",
        "Y":"43",
        "Z":"66"
    }
}
```

And a sample of a response with a vector in a more structured format:

```json
{
    "data": {
        "getVector": {
            "X":"23",
            "Y":"43",
            "Z":"66"
        }
    }
}
```

### 1. Change `ParseValue` to accept strings or structured data:

```csharp
// In Vector3Type
public override object ParseValue(object value)
{
    if (value == null)
        return null;

    if (value is string vector3InputString)
    {
        try
        {
            var vector3Parts = vector3InputString.Split(',');
            var x = float.Parse(vector3Parts[0]);
            var y = float.Parse(vector3Parts[1]);
            var z = float.Parse(vector3Parts[2]);
            return new Vector3(x, y, z);
        }
        catch
        {
            throw new FormatException($"Failed to parse {nameof(Vector3)} from input '{vector3InputString}'. Input should be a string of three comma-separated floats in X Y Z order, ex. 1.0,2.0,3.0");
        }
    }

    if (value is IDictionary<string, object> dictionary)
    {
        try
        {
            var x = Convert.ToSingle(dictionary["x"]);
            var y = Convert.ToSingle(dictionary["y"]);
            var z = Convert.ToSingle(dictionary["z"]);
            if (dictionary.Count > 3)
                return ThrowValueConversionError(value);
            return new Vector3(x, y, z);
        }
        catch
        {
            throw new FormatException($"Failed to parse {nameof(Vector3)} from object. Input should be an object of three floats named X Y and Z");
        }
    }

    return ThrowValueConversionError(value);
}
```

### 2. Change `Serialize` to return structured data.

```csharp
// In Vector3Type

public override object Serialize(object value)
{
    if (value == null)
        return null;

    if (value is Vector3 vector3)
        return vector3;

    return ThrowSerializationError(value);
}
```

### 3. Change `ToAST` to return an AST literal that represents the data.

Since `Serialize` no longer returns a type that can be converted to an AST node, it is
necessary to override this method.

```csharp
// In Vector3Type

public override IValue ToAST(object value)
{
    if (value == null)
        return new NullValue();

    if (value is Vector3 vector3)
    {
        return new StringValue($"{vector3.X},{vector3.Y},{vector3.Z}");
    }

    return ThrowASTConversionError(value);
}
```

With these changes, a variable can be parsed as a string or as a structured object, and is always returned
as a structured object. Again, note that as an input literal, it must be parsed as a string.

## ValueConverter

When `GetArgument<T>` is called, the argument value is coerced to the requested type via the `ValueConverter`.
No conversion takes place when the requested type matches the type of the object or scalar (the type returned from
`ParseLiteral` or `ParseValue`). But you can also use the value converter to assist with input deserialization.

For instance, you may be using `IdGraphType` within your schema as unique identifiers for your data objects. Pursuant
to the GraphQL specification, these identifiers should be passed as strings such as in the below example:

```graphql
{
    widget (id: "3") {
        name
    }
}
```

However, your code may use integer identifiers in the data access layer. So when you call `context.GetArgument<int>("id")`,
GraphQL.NET calls the value converter to convert the string to an integer.

The value converter can be extended globally by calling the static method `Register` as follows:

```csharp
ValueConverter.Register<Vector3, string>(v => $"{v.X},{v.Y},{v.Z}");
```

The above method registers a conversion from the `Vector3` format to a `string`. Since the registration is static,
it should only be done once per application lifetime. For instance, in a static constructor of your schema.

```csharp
public class MySchema : Schema
{
    static MySchema()
    {
        ValueConverter.Register<Vector3, string>(v => $"{v.X},{v.Y},{v.Z}");
    }

    ...
}
```

## Null values

Custom scalars process and handle null values during serialization and deserialization. This allows
for custom scalars that can assist when you have database values such as 0 that should represent null
when exposed outside the schema. Below is an example of a scalar intended to represent a database
autoincrementing numeric identifier internally, where null values are stored as 0.

```csharp
public class DbIdGraphType : ScalarGraphType
{
    public DbIdGraphType()
    {
        Name = "DbId";
    }

    public override object ParseLiteral(IValue value) => value switch
    {
        StringValue s => int.TryParse(s.Value, out int i) && i > 0 ? i : throw new FormatException($"'{s.Value}' is not a valid identifier."),
        NullValue _ => 0,
        _ => ThrowLiteralConversionError(value)
    };

    public override object ParseValue(object value) => value switch
    {
        string s => int.TryParse(s, out int i) && i > 0 ? i : throw new FormatException($"'{s}' is not a valid identifier."),
        null => 0,
        _ => ThrowValueConversionError(value)
    };

    public override object Serialize(object value) => value switch
    {
        int i => i > 0 ? i.ToString() : i == 0 ? null : ThrowSerializationError(value),
        _ => ThrowSerializationError(value)
    };
}
```

## Replacing built-in scalar types

In some cases you may want or need to replace the functionality of the built-in graph types. This can
be accomplished by registering a replacement scalar before the schema has been initialized. Keep in
mind that replacing a built-in type may affect the operation of introspection queries.

In order to replace a built-in scalar graph type, the new scalar graph type must:

1. inherit from the scalar graph type it is replacing; and
2. have the `Name` property set to the name of the built-in graph type.

You may then override any of the members to provide custom implementations. Note that most of the
built-in scalars override `CanParseLiteral`, so it may be necessary to override that method if you
override `ParseLiteral`. Check the source code for the built-in scalar type you are overriding for
further reference.

Below is a sample of how to replace the built-in `BooleanGraphType` so it will accept 0 and non-zero
values to represent false and true.

### 1. Create a new scalar graph type `MyBooleanGraphType`. Inherit from `BooleanGraphType` and set
the name to be `Boolean`.

```csharp
public class MyBooleanGraphType : BooleanGraphType
{
    public MyBooleanGraphType()
    {
        Name = "Boolean";
    }
}
```

### 2. Override the methods as necessary; in this case we must override all of them except `IsValidDefault`.

```csharp
public class MyBooleanGraphType : BooleanGraphType
{
    public MyBooleanGraphType()
    {
        Name = "Boolean";
    }

    public override object ParseLiteral(IValue value) => value switch
    {
        BooleanValue b => b.Value,
        IntValue i => ParseValue(i.Value),
        LongValue l => ParseValue(l.Value),
        BigIntValue bi => ParseValue(bi.Value),
        StringValue s => ParseValue(s.Value),
        FloatValue f => ParseValue(f.Value),
        DecimalValue d => ParseValue(d.Value),
        NullValue _ => null,
        _ => ThrowLiteralConversionError(value)
    }

    public virtual bool CanParseLiteral(IValue value)
    {
        try
        {
            _ = ParseLiteral(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override object ParseValue(object value) => value switch
    {
        bool _ => value,
        byte b => b != 0,
        sbyte sb => sb != 0,
        short s => s != 0,
        ushort us => us != 0,
        int i => i != 0,
        uint ui => ui != 0,
        long l => l != 0,
        ulong ul => ul != 0,
        BigInteger bi => bi != 0,
        float f => f != 0,
        double d => d != 0,
        decimal d => d != 0,
        string s => bool.Parse(s),
        null => null,
        _ => ThrowValueConversionError(value)
    }

    public virtual bool CanParseValue(object value)
    {
        try
        {
            _ = ParseValue(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public virtual object Serialize(object value) => ParseValue(value);

    public override IValue ToAST(object value) => Serialize(value) switch
    {
        bool b => new BooleanValue(b),
        null => new NullValue(),
        _ => ThrowASTConversionError(value)
    };
}
```

### 3. Register the custom scalar within your schema.

The final step is to register an instance of the custom scalar to the schema. This can be
done for code-first or schema-first schemas. For code-first schemas, register it within
your constructor via `RegisterType`, as follows:

```csharp
public class MySchema : Schema
{
    public void MySchema()
    {
        Query = ....;

        RegisterType(new MyBooleanGraphType());
    }
}
```

For schema-first schemas, register it immediately after calling `Schema.For` to create the schema.

```csharp
var schema = Schema.For(...);
schema.RegisterType(new MyBooleanGraphType());
```

Now all `BooleanGraphType` references in your schema will utilize the new `MyBooleanGraphType`
registered with the schema. This technique can be used to replace any of the built-in graph types.

Note that if you set the `ResolvedType` property of a field or argument to an instance of a built-in
type, or provide an instance of a built-in type to an applicable constructor, it will not be replaced
with your registered replacement built-in type. For example, consider this code:

```csharp
Field<StringGraphType>("sample",
    arguments: new QueryArguments {
        // will be replaced with MyBooleanGraphType
        new QueryArgument<BooleanGraphType> { Name = "argNewBehavior" }

        // will retain default behavior
        new QueryArgument(new BooleanGraphType()) { Name = "argOldBehavior" }
    },
    resolve: ...);
```
