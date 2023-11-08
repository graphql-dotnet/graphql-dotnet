# GQL002: `Name` method invocation can be removed

|                        | Value   |
| ---------------------- | ------- |
| **Rule ID**            | GQL002  |
| **Category**           | Usage   |
| **Default severity**   | Warning |
| **Enabled by default** | Yes     |
| **Code fix provided**  | Yes     |

## Cause

The same name is provided in `Field`, `Connection` or `ConnectionBuilder.Create` and `Name` methods.

## Rule description

Field name should be provided in the `Field`, `Connection` or `ConnectionBuilder.Create` method. The `Name` method call is unnecessary and can be removed.

## How to fix violations

Remove the `Name` method call.

## Example of a violation

```c#
Field<StringGraphType>("Name").Name("Name");
Connection<StringGraphType>("Name").Name("Name");
ConnectionBuilder<string>.Create<StringGraphType>("Name").Name("Name");
ConnectionBuilder.Create<StringGraphType, string>("Name").Name("Name");
```

## Example of how to fix

```c#
Field<StringGraphType>("Name");
Connection<StringGraphType>("Name");
ConnectionBuilder<string>.Create<StringGraphType>("Name");
ConnectionBuilder.Create<StringGraphType, string>("Name");
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL002
// The code that's violating the rule is on this line.
#pragma warning restore GQL002
```

To disable the rule for a file, folder, or project, set its severity to `none` in the [configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL002.severity = none
```

For more information, see [How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules

[GQL001: Define the name in `Field`, `Connection` or `ConnectionBuilder.Create` method](/GQL001_DefineTheNameInFieldMethod)  
[GQL003: Different names defined by `Field`, `Connection` or `ConnectionBuilder.Create` and `Name` methods](/GQL003_DifferentNamesDefinedByFieldAndNameMethods)
