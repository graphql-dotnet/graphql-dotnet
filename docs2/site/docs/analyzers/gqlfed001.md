# GQLFED001: Field does not exist

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED001  |
| **Category**           | Federation |
| **Default severity**   | Error      |
| **Enabled by default** | Yes        |
| **Code fix provided**  | No         |
| **Introduced in**      | v9.0       |

## Cause

A field specified in a Federation directive (such as `@key`) does not exist on the GraphQL type.

## Rule description

Federation directives like `@key` reference fields on a GraphQL type. All field names
specified in these directives must exist on the type being decorated.

This analyzer validates that all fields referenced in Federation directive method calls
(such as `.Key()`) exist on the GraphQL type.

## How to fix violations

Either add the missing field to the type or correct the field name in the directive method call.

## Example of a violation

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id nickname");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

## Example of how to fix

**Option 1: Correct the field name**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id name");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

**Option 2: Add the missing field**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id nickname");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
        Field<NonNullGraphType<StringGraphType>>("nickname");
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED001
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED001
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED001.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
