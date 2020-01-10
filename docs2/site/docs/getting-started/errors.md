# Error Handling

The `ExecutionResult` provides an `Errors` property which includes any errors encountered
during execution. Errors are returned [according to the spec](https://graphql.github.io/graphql-spec/June2018/#sec-Errors),
which means stack traces are excluded. The `ExecutionResult` is transformed to what the spec
requires using one or the other `IDocumentWriter`. For example `GraphQL.NewtonsoftJson.DocumentWriter`
uses JSON.NET and `GraphQL.SystemTextJson.DocumentWriter` uses new .NET Core memory optimized
serializer from [`System.Text.Json`](https://docs.microsoft.com/en-us/dotnet/api/system.text.json).
For JSON.NET you can change what information is provided by setting your `ContractResolver` in
`JsonSerializerSettings` passed to `DocumentWriter` constructor.

To help debug errors, you can set `ExposeExceptions` on `ExecutionOptions` which will expose error stack traces.

```csharp
var executor = new DocumentExecutor();
ExecutionResult result = await executor.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.ExposeExceptions = true;
});
```

You can throw an `ExecutionError` error in your resolver and it will be caught
and displayed. You can also add errors to the `ResolveFieldContext.Errors` directly.

```csharp
Field<DroidType>(
  "hero",
  resolve: context => context.Errors.Add(new ExecutionError("Error Message"))
);

Field<DroidType>(
  "hero",
  resolve: context => throw new ExecutionError("Error Message")
);
```

Also `ExecutionOptions.UnhandledExceptionDelegate` allows you to override, hide,
modify or just log the unhandled exception from your resolver before wrap it into
`ExecutionError`. This can be useful for hiding error messages that reveal server
implementation details.

You can provide additional error handling or logging for fields by adding Field Middleware.
