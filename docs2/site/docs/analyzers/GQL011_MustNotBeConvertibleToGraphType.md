# GQL011: The type must not be convertible to `IGraphType`

|                        | Value  |
| ---------------------- | ------ |
| **Rule ID**            | GQL011 |
| **Category**           | Usage  |
| **Default severity**   | Error  |
| **Enabled by default** | Yes    |
| **Code fix provided**  | No     |

## Cause

This rule triggers when a type implementing `IGraphType` used as the type
argument when the type argument must NOT be convertible to `IGraphType`.

## Rule description

Multiple generic types and methods in the GraphQL library receive unconstrained
type arguments. This diagnostic rule simulates `where T : not IGraphType`
constraint which is not supported in .NET.

## How to fix violations

Use appropriate type argument.

## Example of a violation

In the following example the `PersonGraphType` is incorrectly used as the type
argument of the `InterfaceGraphType<TSource>` type.

```c#
public class MyInterfaceGraphType : InterfaceGraphType<PersonGraphType>
{
}

public class PersonGraphType : ObjectGraphType<Person>
{
}

public class Person { }
```

In this example, the `StringGraphType` is incorrectly used as the return type of
the resolver

```c#
public class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        Field<StringGraphType>("name")
            .Returns<StringGraphType>()
            .Resolve(context => null);
    }
}
```

## Example of how to fix

Use `Person` as the argument type of the `InterfaceGraphType<TSource>` type

```c#
public class MyInterfaceGraphType : InterfaceGraphType<Person>
{
}

public class PersonGraphType : ObjectGraphType<Person>
{
}

public class Person { }
```

Use `string` type as the return type of the resolver

```c#
public class MyGraphType : ObjectGraphType
{
    public MyGraphType()
    {
        Field<StringGraphType>("name")
            .Returns<StringGraphType>()
            .Resolve(context => null);
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL011
// The code that's violating the rule is on this line.
#pragma warning restore GQL011
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL011.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules
