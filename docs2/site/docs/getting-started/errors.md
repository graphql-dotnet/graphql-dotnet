# Error Handling

The `ExecutionResult` provides an `Errors` property which includes any errors encountered during execution.  Errors are returned [according to the spec](http://facebook.github.io/graphql/June2018/#sec-Errors), which means stack traces are excluded.  The `ExecutionResult` is transformed to what the spec requires using JSON.NET.  You can change what information is provided by overriding the JSON Converter.

To help debug errors, you can set `ExposeExceptions` on `ExecutionOptions` which will expose error stack traces.

```csharp
var executor = new DocumentExecutor();
ExecutionResult result = await executor.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.ExposeExceptions = true;
});
```

You can throw an `ExecutionError` error in your resolver and it will be caught and displayed.  You can also add errors to the `IResolveFieldContext.Errors` directly.

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

You can provide additional error handling or logging for fields by adding Field Middleware.
