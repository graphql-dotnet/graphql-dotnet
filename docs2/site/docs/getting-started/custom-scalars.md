# Custom Scalars

You can extend your schema with your own custom scalars. Conceptually, a scalar must implement the following operations:

- Serialization: Transforms a scalar from its server-side representation to a representation suitable for the client.

- Value Parsing: Transforms a scalar from its client-side representation as a variable to its server-side representation.

- Literal Parsing: Transforms a scalar from its client-side representation as an argument to its server-side representation.

Parsing for arguments and variables are handled separately because while arguments must always be expressed in GraphQL query syntax, variable format is transport-specific (usually JSON).

The following example shows how to create a custom scalar in GraphQL.NET. You will create a 3D Vector which will be exchanged between server and client as a comma-separated string (ex. "34, 61, 12"). The example assumes the GraphQL schema is implemented in an ASP.NET Core project using the `Microsoft.Extensions.DependencyInjection` package, though only minor modifications would be required for other project types.

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

Vectors should be received in a more structured format:

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

1. Create the class for the server-side representation.

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

2. Create a graph type for the scalar by inheriting `ScalarGraphType`.

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

3. Register the graph type with the DI container.

```csharp
// In Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<Vector3Type>();

    // Other schema registrations...
}
```

4. Prepare to accept `Vector3` inputs from query arguments. Implement `ScalarGraphType.ParseLiteral`.

```csharp
//in Vector3Type

public override object ParseLiteral(IValue value)
{
    return value is StringValue stringValue
        ? ParseValue(stringValue.Value)
        : null;
}
```

Once the raw string is extracted from the value node, normal parsing can proceed.

5. Prepare to accept `Vector3` inputs from query variables. Implement `ScalarGraphType.ParseValue`.

```csharp
// In Vector3Type
public override object ParseValue(object value)
{
    return ValueConverter.ConvertTo(value, typeof(Vector3));
}
```

For this call to succeed, a type conversion from `string` to `Vector3` must be registered with the `ValueConverter` class. This can be done anywhere since the API is static. For this example, perform the registration in the schema's constructor.

```csharp
using GraphQL;
using GraphQL.Language.AST;
using GraphQL.Types;
using StarWars.Types;
using System;

public class ExampleSchema : Schema
{
    public ExampleSchema(IServiceProvider provider)
        : base(provider)
    {        
        ValueConverter.Register<string, Vector3>(ParseVector3);

        // Other schema assignments...
    }

    private Vector3 ParseVector3(string vector3InputString)
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
}
```

6. Prepare to support conversion of `Vector3` to an AST node. This allows GraphQL.NET to treat values parsed from variables the same as arguments, which arrive for execution as AST nodes. 

Implement `ValueNode<Vector3>`. Instances of `Vector3` parsed from variables will be wrapped in this type during execution.

```csharp
public class Vector3Value: ValueNode<Vector3>
{
    public Vector3Value(Vector3 value)
    {
        Value = value;
    }

    protected override bool Equals(ValueNode<Vector3> node)
    {
        return Value.Equals(node.Value);
    }
}
```

Implement `IAstFromValueConverter` for `Vector3`. This type is used to instruct GraphQL.NET how to wrap custom scalars in `IValue` instances during execution. The framework uses `Matches` to find the appropriate AST value converter after parsing a custom scalar, then uses `Convert` to perform the conversion.

```csharp
public class Vector3AstValueConverter : IAstFromValueConverter
{
    public IValue Convert(object value, IGraphType type)
    {
        return new Vector3Value((Vector3)value);
    }

    public bool Matches(object value, IGraphType type)
    {
        return value is Vector3;
    }
}
```

Register `Vector3AstValueConverter` with the schema. Don't conflate `ValueConverter.Register` used in step 4 with `Schema.RegisterValueConverter` - the latter is used for conversions to AST value nodes.

```csharp
// In ExampleSchema
public ExampleSchema(IServiceProvider provider)
    : base(provider)
{        
    ValueConverter.Register<string, Vector3>(ParseVector3);
    RegisterValueConverter(new Vector3AstValueConverter());

    // Other schema assignments...
}
``` 

Update the implementation of `Vector3Type.ParseLiteral`:

```csharp
// In Vector3Type
public override object ParseLiteral(IValue value)
{
    // new test
    if (value is Vector3Value vector3Value)
        return ParseValue(vector3Value.Value);

    return value is StringValue stringValue
        ? ParseValue(stringValue.Value)
        : null;
}

```

This is necessary since the query executor converts all arguments and variables to `IValue` instances before coercing them to their server-side representation using `ParseLiteral`. `Vector3` instances parsed from variables will be converted to the more specific `Vector3Value` type.

7. Implement `ScalarGraphType.Serialize` so `Vector3` instances can be sent to the client.

```csharp
// In Vector3Type

public override object Serialize(object value)
{
    return ValueConverter.ConvertTo(value, typeof(Vector3));
}
```

This implementation may surprise you. Why is `Serialize`, which is used for output, implemented identically to `ParseValue`, which is used for input? Why does `Serialize` return an object, rather than a string or byte array? It helps to understand a few internals of the library.

- `Serialize` will be called during query execution, and should be passed an instance of `Vector3` from a field resolver

- `Serialize` is _also_ called when reading variables from the client so that variables can be converted to `IValue` instances. In the case of `Vector3Type`, `value` will be a string during this process.

- `ValueConverter.ConvertTo` handles the case when `value` is an instance of the requested type by returning `value`. Therefore, `ValueConverter.ConvertTo` neatly handles both input and output representations of the scalar.

- Since GraphQL specifies no response format, `Serialize` is not responsible for preparing the scalar for transport to the client. It is only responsible for generating an object which can eventually be serialized by `IDocumentWriter` or other transport-focused API.

In this example, you created a custom scalar. In summary:

- Create a class for the server-side representation of the scalar
- Implement a `ScalarGraphType` which handles parsing, literal parsing, and serialization
- Register the `ScalarGraphType with the DI container
- Define how to parse the raw representation of the scalar to its server-side representation using `ValueConverter.Register`
- Implement a `ValueNode<T>` class for the server-side representation
- Implement an `IAstFromValueConverter` for wrapping the server-side representation in its `ValueNode<T>` implementation
- Register the `IAstFromValueConverter` with the schema using `Schema.RegisterValueConverter`
