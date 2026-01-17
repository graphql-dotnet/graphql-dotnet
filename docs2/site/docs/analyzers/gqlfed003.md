# GQLFED003: Duplicate key

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED003  |
| **Category**           | Federation |
| **Default severity**   | Warning    |
| **Enabled by default** | Yes        |
| **Code fix provided**  | Yes        |
| **Introduced in**      | v9.0       |

## Cause

Multiple `@key` directives with identical field specifications are defined on the same GraphQL type.

## Rule description

The `@key` directive designates an object type as an entity and specifies its key fields. While a type can have multiple keys to support different identification patterns, defining the same key multiple times is redundant and likely a mistake.

This analyzer detects duplicate keys by normalizing field names (case-insensitive) and field order. For example, `"id name"` and `"name id"` are considered duplicates because they specify the same set of fields.

## How to fix violations

Remove the duplicate `.Key()` method calls, keeping only one instance of each unique key.

## Example of a violation

**Example 1: Exact duplicate**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");
        this.Key("id");  // Duplicate
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

**Example 2: Different field order**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id name");
        this.Key("name id");  // Duplicate (same fields, different order)
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

**Example 3: Nested fields**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("organization { id name }");
        this.Key("organization { name id }");  // Duplicate (nested fields, different order)
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<OrganizationGraphType>>("organization");
    }
}
```

## Example of how to fix

**Fix Example 1: Remove exact duplicate**

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

**Fix Example 2: Keep one version of the key**

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

**Fix Example 3: Multiple distinct keys (valid)**

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");                      // First key: by id alone
        this.Key("email");                   // Second key: by email alone
        this.Key("organization { id } name"); // Third key: by org id and name
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
        Field<NonNullGraphType<StringGraphType>>(x => x.Email);
        Field<NonNullGraphType<OrganizationGraphType>>("organization");
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED003
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED003
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED003.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
