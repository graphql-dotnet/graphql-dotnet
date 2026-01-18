# GQLFED004: Redundant key

|                        | Value      |
| ---------------------- | ---------- |
| **Rule ID**            | GQLFED004  |
| **Category**           | Federation |
| **Default severity**   | Warning    |
| **Enabled by default** | Yes        |
| **Code fix provided**  | Yes        |
| **Introduced in**      | v9.0       |

## Cause

A key specified in a `@key` directive is redundant because another key with fewer fields already exists that can uniquely identify the entity.

## Rule description

In Apollo Federation, a key is a set of fields that uniquely identifies an instance of an entity. When multiple keys are defined on a type, each key should provide a distinct way to identify the entity. A key is considered redundant if it contains all the fields of another key plus additional fields, because the smaller key is already sufficient for unique identification.

This analyzer detects when a key is a superset of another key (contains all fields of another key plus more), which makes it redundant and unnecessary.

## How to fix violations

Remove the redundant key declaration. The smaller, more efficient key is sufficient for entity resolution.

## Example of a violation

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");
        this.Key("id name"); // Redundant: "id" alone is sufficient
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
    }
}
```

In this example, the key `"id name"` is redundant because the key `"id"` already uniquely identifies the user. The additional `name` field doesn't add value for entity resolution.

## Example of how to fix

Remove the redundant key:

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

## More examples

### Nested field redundancy

```csharp
// Violation
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("organization { id }");
        this.Key("organization { id name }"); // Redundant
        
        Field<NonNullGraphType<IdGraphType>>("id");
        Field<NonNullGraphType<OrganizationGraphType>>("organization");
    }
}

// Fixed
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("organization { id }");
        
        Field<NonNullGraphType<IdGraphType>>("id");
        Field<NonNullGraphType<OrganizationGraphType>>("organization");
    }
}
```

### Multiple redundant keys

```csharp
// Violation
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");
        this.Key("id name");     // Redundant
        this.Key("id name email"); // Redundant
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
        Field<NonNullGraphType<StringGraphType>>(x => x.Email);
    }
}

// Fixed
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
        Field<NonNullGraphType<StringGraphType>>(x => x.Email);
    }
}
```

## When keys are NOT redundant

Keys are not redundant when they represent different ways to identify an entity:

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id");        // Can look up by ID
        this.Key("email");     // Can also look up by email
        this.Key("username");  // Can also look up by username
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Email);
        Field<NonNullGraphType<StringGraphType>>(x => x.Username);
    }
}
```

Similarly, keys with partial overlap are not redundant:

```csharp
public class UserGraphType : ObjectGraphType<User>
{
    public UserGraphType()
    {
        this.Key("id name");   // Composite key: ID + name
        this.Key("id email");  // Composite key: ID + email
        
        // These are different composite keys, neither is a subset of the other
        
        Field<NonNullGraphType<IdGraphType>>(x => x.Id);
        Field<NonNullGraphType<StringGraphType>>(x => x.Name);
        Field<NonNullGraphType<StringGraphType>>(x => x.Email);
    }
}
```

## Suppress a warning

If you just want to suppress a single violation, add preprocessor directives to
your source file to disable and then re-enable the rule.

```csharp
#pragma warning disable GQLFED004
// The code that's violating the rule is on this line.
#pragma warning restore GQLFED004
```

To disable the rule for a file, folder, or project, set its severity to `none`
in the
[configuration file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files).

```ini
[*.cs]
dotnet_diagnostic.GQLFED004.severity = none
```

For more information, see
[How to suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).
