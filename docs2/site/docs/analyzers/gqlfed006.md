# GQLFED006: Key field must not be an interface or union type

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED006  |
| **Category**           | Federation |
| **Default severity**   | Error      |
| **Enabled by default** | Yes        |
| **Code fix provided**  | No         |
| **Introduced in**      | v9.0       |

## Cause

A field specified in a `@key` directive returns an interface or union type.

## Rule description

The `@key` directive designates an object type as an entity and specifies its
key fields. Key fields are a set of fields that a subgraph can use to uniquely
identify any instance of the entity. Key fields must return concrete object
types or scalar types, not abstract types like interfaces or unions.

This analyzer validates that all fields referenced in `.Key()` method calls
do not return interface or union types. While these fields can be nested within
object type fields, they themselves cannot be the terminal fields in a key
selection set.

## How to fix violations

Replace the interface or union field with a concrete field that can uniquely
identify the entity. Common solutions include:

- Use a specific ID field instead of an interface field
- Reference nested fields within object types
- Use scalar fields for identification

## Example of a violation

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        // Profile is an interface type
        this.Key("profile");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<ProfileInterfaceType>>("profile");
    }
}

public class ProfileInterfaceType : InterfaceGraphType<IProfile>
{
    public ProfileInterfaceType()
    {
        Field<NonNullGraphType<StringGraphType>>("displayName");
    }
}
```

## Example of how to fix

**Option 1: Use a concrete ID field**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        // Use a specific ID field
        this.Key("id");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<ProfileInterfaceType>>("profile");
    }
}
```

**Option 2: Use nested object fields**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        // Reference a field within an object type
        this.Key("account { id }");

        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<AccountGraphType>>("account");
        Field<NonNullGraphType<ProfileInterfaceType>>("profile");
    }
}

public class AccountGraphType : ObjectGraphType<Account>
{
    public AccountGraphType()
    {
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED006
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED006
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED006.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
