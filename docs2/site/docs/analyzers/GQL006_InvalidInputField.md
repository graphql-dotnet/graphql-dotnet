# GQL006: Invalid input field

|                        | Value   |
| ---------------------- | ------- |
| **Rule ID**            | GQL006  |
| **Category**           | Usage   |
| **Default severity**   | Warning |
| **Enabled by default** | Yes     |
| **Code fix provided**  | No      |

## Cause

This rule triggers when a field defined on a type deriving from `InputObjectGraphType<TSourceType>` cannot be mapped to the `TSourceType` type.

## Rule description

This diagnostic is reported when the input field name cannot be mapped to any field, property, or constructor parameter of the `TSourceType` type. The name comparison is case-insensitive. The input field can be mapped when all the following rules are respected:

### Mapping rules for property/field

- Must have the exact same name (case-insensitive)
- Must be public
- Must not be static
- Must be settable (have a public setter / not a constant)

### Mapping rules for constructor parameter

- Must have the exact same name (case-insensitive)
- Constructor must be public

## How to fix violations

Follow the mapping rules described in the [Rule description](#rule-description) section or remove the invalid input field.

## Example of a violation

The following example shows an input field named `FirstName` and a source type with a property named `Name`:

```c#
public class MyInputGraphType : InputObjectGraphType<MySourceType>
{
    public MyInputGraphType()
    {
        Field<StringGraphType>("FirstName");
    }
}

public class MySourceType
{
    public string Name { get; set; }
}
```

## Example of how to fix

**Option 1**: rename the input field name to match the source type property name

```c#
public class MyInputGraphType : InputObjectGraphType<MySourceType>
{
    public MyInputGraphType()
    {
        Field<StringGraphType>("Name");
    }
}

public class MySourceType
{
    public string Name { get; set; }
}
```

**Option 2**: rename the source type property name to match the input field name

```c#
public class MyInputGraphType : InputObjectGraphType<MySourceType>
{
    public MyInputGraphType()
    {
        Field<StringGraphType>("FirstName");
    }
}

public class MySourceType
{
    public string FirstName { get; set; }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL006
// The code that's violating the rule is on this line.
#pragma warning restore GQL006
```

To disable the rule for a file, folder, or project, set its severity to `none` in the [configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL006.severity = none
```

For more information, see [How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules
