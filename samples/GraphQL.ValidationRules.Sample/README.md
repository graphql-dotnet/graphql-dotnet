# GraphQL.ValidationRules.Sample

This sample demonstrates how to register and run a custom validation rule that blocks access to a specific field.

## What it shows

- Building a schema with two query fields
- Implementing a custom `ValidationRuleBase`
- Running GraphQL execution with `DocumentValidator.CoreRules` plus a custom rule
- Returning a validation error when the blocked field is requested

## Run

```bash
dotnet run --project samples/GraphQL.ValidationRules.Sample/GraphQL.ValidationRules.Sample.csproj
```

Expected behavior:

- Query `{ publicMessage }` passes validation
- Query `{ secretMessage }` returns a validation error from the custom rule
