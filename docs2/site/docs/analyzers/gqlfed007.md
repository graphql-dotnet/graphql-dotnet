# GQLFED007: @shareable directive is not allowed on interface or input type fields

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED007  |
| **Category**           | Federation |
| **Default severity**   | Error      |
| **Enabled by default** | Yes        |
| **Code fix provided**  | No         |
| **Introduced in**      | v9.0       |

## Cause

The `@shareable` directive is applied to a field on an interface type or an
input type.

## Rule description

In Apollo Federation, the `@shareable` directive marks an object type field as
resolvable by multiple subgraphs. The directive is only valid on object type
fields — it cannot be applied to fields on interface types or input object
types.

Applying `@shareable` to an interface field has no meaningful effect in the
federation schema and indicates a misuse of the directive. Applying it to an
input type field is similarly invalid, as input types describe data sent to a
service and are not resolved across subgraphs.

This analyzer validates that `.Shareable()` is only chained onto fields that
belong to an `ObjectGraphType`.

## How to fix violations

Remove the `.Shareable()` call from the field definition. If you intend to mark
a field as shareable, ensure it belongs to an `ObjectGraphType` rather than an
`InterfaceGraphType` or `InputObjectGraphType`.

## Examples of violations

```csharp
public class ProductInterfaceType : InterfaceGraphType<Product>
{
    public ProductInterfaceType()
    {
        Field<NonNullGraphType<StringGraphType>>("name").Shareable(); // GQLFED007
    }
}
```

## Example of how to fix

Remove the `.Shareable()` call.

```csharp
public class ProductInterfaceType : InterfaceGraphType<Product>
{
    public ProductInterfaceType()
    {
        Field<NonNullGraphType<StringGraphType>>("name"); // no @shareable
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED007
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED007
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED007.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
