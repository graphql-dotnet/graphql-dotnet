# GraphQL.CustomValidationRule.Sample

This sample shows how to add a custom validation rule that reads field metadata and request user context before execution.

The sample schema exposes two fields:

| Field | Behavior |
| --- | --- |
| `publicReport` | Always passes validation. |
| `adminReport` | Requires the `reports:read` scope in `ExecutionOptions.UserContext`. |

Run the sample from the repository root:

```bash
dotnet run --project samples/GraphQL.CustomValidationRule.Sample/GraphQL.CustomValidationRule.Sample.csproj
```

Expected behavior:

- `{ publicReport }` passes validation without scopes.
- `{ adminReport }` returns a validation error without the `reports:read` scope.
- `{ adminReport }` passes validation when `reports:read` is supplied.
