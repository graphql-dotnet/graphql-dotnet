# Custom Validation Rules Sample

This sample demonstrates how to create custom validation rules for GraphQL.NET v8.

## What's Included

| File | Description |
|------|-------------|
| `Program.cs` | Main program with runnable examples |
| `Rules/RequiresAuthenticationRule.cs` | Field-level authentication check (pre-node visitor) |
| `Rules/RoleBasedAccessRule.cs` | Role-based access control using claims (pre-node visitor) |
| `Rules/MaxQueryDepthRule.cs` | Query depth and field count limiting |
| `Rules/ArgumentAndVariableValidationRules.cs` | Post-node visitor and variable visitor examples |
| `CUSTOM_VALIDATION_RULES.md` | Comprehensive documentation |

## Quick Start

```bash
dotnet run
```

## Key Concepts Demonstrated

### 1. Pre-Node Visitor — Access Control
Fields marked with `"RequiresAuth"` metadata reject unauthenticated requests.

### 2. Stateful Visitor — Query Depth Limit
Tracks nesting depth across field nodes to enforce a maximum depth.

### 3. Post-Node Visitor — Argument Validation
Validates parsed argument values after the variable parsing phase.

### 4. Variable Visitor — Input Validation
Inspects and validates variable values during the parsing phase.

## Registration Patterns

```csharp
// Per-request with core rules
options.ValidationRules = DocumentValidator.CoreRules
    .Append(new RequiresAuthenticationRule());

// DI registration
services.AddGraphQL(b => b
    .AddValidationRule<RequiresAuthenticationRule>());
```

See [CUSTOM_VALIDATION_RULES.md](./CUSTOM_VALIDATION_RULES.md) for the full documentation.
