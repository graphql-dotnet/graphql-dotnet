# GQL008: Don't use an obsolete 'Argument' method

|                        | Value   |
| ---------------------- | ------- |
| **Rule ID**            | GQL008  |
| **Category**           | Usage   |
| **Default severity**   | Warning |
| **Enabled by default** | Yes     |
| **Code fix provided**  | Yes     |

## Cause

This rule triggers when the obsolete `Argument<TArgumentGraphType, TArgumentType>()` method with two type parameters was used on the field builder.

## Rule description

The method overload `Argument<TArgumentGraphType, TArgumentType>(name, description, defaultValue, configure)` is obsolete and will be remove in future version.

## How to fix violations

Use `Argument<TArgumentGraphType>()` method overload with a single generic type parameter. Use `Action<QueryArgument>` parameter to configure default argument value.

## Example of a violation

```c#

Field<StringGraphType>("Text").Argument<StringGraphType, string>(
    "arg",
    "description",
    "MyDefault");

Field<StringGraphType>("Text").Argument<StringGraphType, string>(
    "arg",
    "description",
    "MyDefault",
    argument => argument.DeprecationReason = "Deprecation Reason");


```

## Example of how to fix

Make the source type fields and properties settable

```c#
Field<StringGraphType>("Text").Argument<StringGraphType>(
    "arg",
    "description",
    argument => argument.DefaultValue = "MyDefault");

Field<StringGraphType>("Text").Argument<StringGraphType>(
    "arg",
    "description",
    argument =>
    {
        argument.DeprecationReason = "Deprecation Reason";
        argument.DefaultValue = "MyDefault";
    });

```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL008
// The code that's violating the rule is on this line.
#pragma warning restore GQL008
```

To disable the rule for a file, folder, or project, set its severity to `none` in the [configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL008.severity = none
```

For more information, see [How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Related rules
