# GQL004: Don't use obsolete `Field` methods

|                        | Value   |
| ---------------------- | ------- |
| **Rule ID**            | GQL004  |
| **Category**           | Usage   |
| **Default severity**   | Warning |
| **Enabled by default** | Yes     |
| **Code fix provided**  | Yes     |

## Cause

One of the deprecated `FieldXXX` methods that return `FieldType` were used.

## Rule description

A bunch of `FieldXXX` APIs were deprecated and will be removed in future version. For more information see [v7 Migration Guide](../migrations/migration7/#11-a-bunch-of-fieldxxx-apis-were-deprecated).

## How to fix violations

You will need to change a way of setting fields on your graph types. Instead of many `FieldXXX` overloads, start configuring your field with one of the `Field` methods defined on `ComplexGraphType`. All such methods define a new field and return an instance of `FieldBuilder<T,U>`. Then continue to configure the field with rich APIs provided by the returned builder.

## Example of a violation

```c#
Field<NonNullGraphType<StringGraphType>>(
    "name",
    "Field description",
    resolve: context => context.Source!.Name);

FieldAsync<CharacterInterface>(
    "hero",
    resolve: async context => await data.GetDroidByIdAsync("3"));

FieldAsync<HumanType>(
    "human",
    arguments: new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
    {
        Name = "id",
        Description = "id of the human"
    }),
    resolve: async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id"))
);
```

## Example of how to fix

```c#
Field<NonNullGraphType<StringGraphType>>("name")
    .Description("Field description")
    .Resolve(context => context.Source!.Name);

Field<CharacterInterface>("hero")
    .ResolveAsync(async context => await data.GetDroidByIdAsync("3"));

Field<HumanType>("human")
    .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
    .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")));

```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQL004
// The code that's violating the rule is on this line.
#pragma warning restore GQL004
```

To disable the rule for a file, folder, or project, set its severity to `none` in the [configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQL004.severity = none
```

For more information, see [How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## Configure code fix

The given diagnostic rule offers an automatic code fix. By default, it attempts to retain the original user code formatting as much as possible but it will remove unnecessary `null` values. For instance, consider the subsequent code snippet:

```c#
Field<StringGraphType>("name", "description", null,
    context => "text");
```

This will be transformed into:

```c#
Field<StringGraphType>("name").Description("description")
    .Resolve(context => "text");
```

### Configure formatting

The `reformat` configuration option will guide the code fix to apply code reformatting. The default value is `false`.

```ini
[*.cs]
dotnet_diagnostic.GQL004.reformat = true
```

The earlier code snippet will undergo the following reformatting:

```c#
Field<StringGraphType>("name")
    .Description("description")
    .Resolve(context => "text");
```

### Configure `null` values handling

The `skip_nulls` option can be set to `false` to preserve `null` values assignments. The default value if `true`.

```ini
[*.cs]
dotnet_diagnostic.GQL004.skip_nulls = false
```

The earlier code snippet will undergo the following reformatting:

```c#
Field<StringGraphType>("name").Description("description").Arguments(null)
    .Resolve(context => "text");
```

## Related rules
