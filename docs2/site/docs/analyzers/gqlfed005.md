# GQLFED005: Key field must not have arguments

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED005  |
| **Category**           | Federation |
| **Default severity**   | Error      |
| **Enabled by default** | Yes        |
| **Code fix provided**  | No         |
| **Introduced in**      | v9.0       |

## Cause

A field specified in a `@key` directive is defined with arguments in the GraphType.

## Rule description

The `@key` directive designates an object type as an entity and specifies its
key fields. Key fields are a set of fields that a subgraph can use to uniquely
identify any instance of the entity. Key fields must not have arguments because
they should represent a stable, deterministic value that can be used for entity
resolution across subgraphs.

This analyzer validates that fields used as key fields are not defined with arguments
in the GraphType.

## How to fix violations

Either:
- Use a different field without arguments for the key, or
- Create a computed field that doesn't require arguments, or
- Remove the arguments from the field definition

## Examples of violations

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("name");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>("name")
            .Argument<IntGraphType>("limit");
    }
}
```

## Examples of how to fix

**Option 1: Use a different field for the key**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id userId");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.UserId);
        Field<NonNullGraphType<StringGraphType>>("name")
            .Argument<IntGraphType>("limit");
    }
}
```

**Option 2: Remove arguments from the field**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("name");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>("name");
    }
}
```

**Option 3: Create a separate field for keying**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("userId");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>("userId")
            .Resolve(context => context.Source.UserId);
        Field<NonNullGraphType<StringGraphType>>("name")
            .Argument<IntGraphType>("limit");
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED005
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED005
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED005.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
