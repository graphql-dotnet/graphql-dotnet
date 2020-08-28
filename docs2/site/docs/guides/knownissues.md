# Known Issues / FAQ

## Common Errors

### Synchronous operations are disallowed.

> System.InvalidOperationException: Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead

ASP.Net Core does not by default allow synchronous reading of input streams. When using the `Newtonsoft.Json` package,
you will need to set the `AllowSynchronousIO` property to `true`. The `System.Text.Json` package fully supports
asynchronous reading of json data streams and should not be a problem.

Here is the workaround for `Newtonsoft.Json`:

```csharp
// kestrel
services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

// IIS
 services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
```

