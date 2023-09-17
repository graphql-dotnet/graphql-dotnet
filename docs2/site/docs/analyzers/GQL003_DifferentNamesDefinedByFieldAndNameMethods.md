# GQL003: Different names defined by `Field` and `Name` methods

|                        | Value        |
| ---------------------- | ------------ |
| **Rule ID**            | GQL003       |
| **Category**           | Deprecations |
| **Default severity**   | Warning      |
| **Enabled by default** | Yes          |
| **Code fix provided**  | Yes          |

## Cause

`Field` and `Name` methods define different field names.

## Rule description

Field name should be provided in the `Field` method. If you intend to use a different name, modify it within the `Field` method instead of using the `Name` method.

## How to fix violations

Specify the preferred field name within the `Field` method and remove the use of the `Name` method.

## Example of a violation

```c#
Field<StringGraphType>("Name1").Name("Name2");
```

## Example of how to fix

```c#
Field<StringGraphType>("Name1");
```

or

```c#
Field<StringGraphType>("Name2");
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL003
// The code that's violating the rule is on this line.
#pragma warning restore GQL003
```

To disable the rule for a file, folder, or project, set its severity to `none` in the [configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL003.severity = none
```

For more information, see [How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules

[GQL001: Define the name in `Field` method](/GQL001_DefineTheNameInFieldMethod)  
[GQL002: `Name` method invocation can be removed](/GQL002_NameMethodInvocationCanBeRemoved)
