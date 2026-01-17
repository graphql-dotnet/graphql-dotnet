# GQLFED002: Key must not be null or empty

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED002  |
| **Category**           | Federation |
| **Default severity**   | Error      |
| **Enabled by default** | Yes        |
| **Code fix provided**  | No         |
| **Introduced in**      | v9.0       |

## Cause

A `@key` directive has been specified with a null or empty `fields` argument.

## Rule description

The `@key` directive designates an object type as an entity and specifies its key fields. The `fields` argument is required and must contain at least one field name. An empty or whitespace-only key is not valid and will not properly identify the entity.

This analyzer validates that all `.Key()` method calls have a non-empty `fields` argument.

## How to fix violations

Provide a valid field name or set of field names in the `.Key()` method call.

## Example of a violation

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("");
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

## Example of how to fix

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED002
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED002
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED002.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
