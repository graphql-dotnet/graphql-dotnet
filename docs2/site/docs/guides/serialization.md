# JSON Serialization and Deserialization

Two libraries are available for assistance deserializing JSON-formatted GraphQL requests,
and serializing GraphQL responses in a JSON format. It is not mandatory to use JSON for the
request or response format, but it is common to do so. The two libraries are:

* [GraphQL.SystemTextJson](https://www.nuget.org/packages/GraphQL.SystemTextJson), for use with the `System.Text.Json` library, and
* [GraphQL.NewtonsoftJson](https://www.nuget.org/packages/GraphQL.NewtonsoftJson), for use with the `Newtonsoft.Json` library

These two projects have very similar classes and extension methods available. There are two notable
differences between the two serialization engines. First, the `Newtonsoft.Json` library does not provide asynchronous
serialization or deserialization methods. Due to this reason, the async `GraphQL.NewtonsoftJson` serialization
helper actually performs synchronous calls on the underlying stream when writing the JSON output. This is
significant when hosting the service via ASP.NET Core, as it is required to deliberately allow synchronous
reading and writing of the underlying stream. A sample of the required configuration is below:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // If using Kestrel:
    services.Configure<KestrelServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
    });

    // If using IIS:
    services.Configure<IISServerOptions>(options =>
    {
        options.AllowSynchronousIO = true;
    });
}
```

The above configuration options are not necessary with the `System.Text.Json` serialization engine.

Second, the `Newtonsoft.Json` library defaults to case-insensitive matching for key names when
deserializing objects. `System.Text.Json` defaults to case-sensitive matching, but converts property
names to camel-case first. Take this example:

```csharp
public class Request
{
    public string Query { get; set; }
    public string OperationName { get; set; }
    public Inputs Variables { get; set; }
}
```

Deserializing the following JSON object is successful with either library:

```json
{
    "query": "query ($arg: Int!) { field1(arg: $arg) { childField } }",
    "variables": {
        "arg": 55
    }
}
```

However, the following JSON object fails with `System.Text.Json`:

```json
{
    "Query": "query ($arg: Int!) { field1(arg: $arg) { childField } }",
    "Variables": {
        "arg": 55
    }
}
```

You can also configure the serialization and deserialization process for your needs
with either library, such as using case insensitive matching with `System.Text.Json`
or enabling or disabling indenting on the serialized output. There are a number of
other differences as well.
[Click this link](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0#table-of-differences-between-newtonsoftjson-and-systemtextjson)
for a comprehensive table of differences between the two serialization engines.

The remainder of the documentation here will assume the use of the `GraphQL.SystemTextJson`
library; slight changes may be necessary if you are using the `GraphQL.NewtonsoftJson` library.

# Deserialization of a GraphQL request and variables

The GraphQL.NET `DocumentExecuter` requires the query and optional operation name as strings,
and the variables (if supplied) deserialized into an `Inputs` object, which is a dictionary of
objects. This dictionary must be deserialized such that the objects it contains are lists (in the form
of `IEnumerable` collections), objects (in the form of `IDictionary<string, object>` collections),
or raw values (e.g. `int`, `long`, `string`, `Guid`, etc). Custom scalars may allow other types as well.

The `InputsConverter` facilitates deserialization of JSON strings into this `Inputs` object as described
above. For the `Newtonsoft.Json` library, it is suggested to disable the automatic conversion of dates
so that the GraphQL.NET scalars can perform this task, enabling proper `DateTimeOffset` deserialization.

You can also use one of the following extension methods to deserialize data with the required options set.

```csharp
public static Inputs ToInputs(this string json);
public static Inputs ToInputs(this JsonElement obj);
public static T FromJson<T>(this string json);
// GraphQL.SystemTextJson only:
public static ValueTask<T> FromJsonAsync<T>(this System.IO.Stream stream, CancellationToken cancellationToken = default);
// GraphQL.NewtonsoftJson only:
public static T FromJson<T>(this System.IO.Stream stream);
```

Here are a couple typical examples:

```csharp
// ASP.NET Core action with multipart/form-data or application/x-www-form-urlencoded encoding
[HttpPost("graphql")]
public IActionResult GraphQL(string query, string operationName, string variables)
{
    var inputs = variables?.ToInputs();
    // execute request and return result
}

// ASP.NET Core action with json encoding
[HttpPost("graphql")]
public IActionResult GraphQL([FromBody] Request request)
{
    // execute request and return result
}

public class Request
{
    string Query { get; set; }
    string OperationName { get; set; }
    JsonElement Variables { get; set; }
}

// Other, with the request as a JSON string
private string Execute(string request)
{
    var request = requestString.FromJson<Request>();
    // execute request and return result
}

public class Request
{
    string Query { get; set; }
    string OperationName { get; set; }
    Inputs Variables { get; set; }
}
```

# Serialization of a GraphQL response

Serialization of a `ExecutionResult` object is handled by `ExecutionResultJsonConverter` which accepts in its
constructor an instance of `IErrorInfoProvider` (see [Error Serialization](#error-serialization) below).
The converter can be registered within an instance of `JsonSerializerOptions` so that serializing an
`ExecutionResult` produces the proper output.

To assist, a `DocumentWriter` class is provided with a single method, `WriteAsync`, which
handles constructing the options, registering the converter, and serializing a specified
`ExecutionResult` to a data stream. This class is designed to be registered as a singleton
within your dependency injection framework, if applicable.

```csharp
// Manually construct an instance
var documentWriter = new DocumentWriter();

// Or register it within your DI framework (Microsoft DI sample below)
services.AddSingleton<IDocumentWriter, DocumentWriter>();
```

Here is an example of the `DocumentWriter`'s use within the `Harness` sample project:

```csharp
private async Task WriteResponseAsync(HttpContext context, ExecutionResult result, CancellationToken cancellationToken)
{
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = 200; // OK

    await _documentWriter.WriteAsync(context.Response.Body, result, cancellationToken);
}
```

You can also write the result to a string with the `WriteToStringAsync` extension method:

```csharp
var resultText = await _documentWriter.WriteToStringAsync(result);
```

## Error Serialization

The GraphQL spec allows for four properties to be returned within each
error: `message`, `locations`, `path`, and `extensions`. The `IDocumentWriter` implementations
provided for the [`Newtonsoft.Json`](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) and
[`System.Text.Json`](https://www.nuget.org/packages/GraphQL.SystemTextJson) packages allow you to control the
serialization of `ExecutionError`s into the resulting json data by providing an `IErrorInfoProvider`
to the constructor of the document writer. The `ErrorInfoProvider` class (default implementation of
`IErrorInfoProvider`) contains 5 properties to control serialization behavior:

* `ExposeExceptionStackTrace` when enabled sets the `message` property for errors to equal the
exception's `.ToString()` method, which includes a stack trace. This property defaults to `false`.
* `ExposeCode` when enabled sets the `extensions`'s `code` property to equal the error's `Code`
property. This property defaults to `true`.
* `ExposeCodes` when enabled sets the `extensions`'s `codes` property to equal a list containing both
the error's `Code` property, if any, and the type name of inner exceptions (after being converted to
UPPER_CASE and removing the "Extension" suffix). So an `ExecutionError` with a code of `INVALID_FORMAT`
that has an inner exception of type `ArgumentNullException` would contain a `codes` property
of `["INVALID_FORMAT", "ARGUMENT_NULL"]`. This property defaults to `true`.
* `ExposeData` when enabled sets the `extension`'s `data` property to equal the data within the error's
`Data` property. This property defaults to `true`.
* `ExposeExtensions` when disabled hides the entire `extensions` property, including `code`, `codes`,
and `data` (if enabled). This property defaults to `true`.

For example, to show the stack traces for unhandled errors during development, you might write code like this:

```csharp
#if DEBUG
    var documentWriter = new DocumentWriter(true, new ErrorInfoProvider(options => options.ExposeExceptionStackTrace = true));
#else
    var documentWriter = new DocumentWriter();
#endif
```

You can also write your own implementation of `IErrorInfoProvider`. For instance, you might want to override
the numerical codes provided by GraphQL.NET for validation errors, reveal stack traces
only to logged-in administrators, or simply add information to the returned error object. Below is a sample
of a custom `IErrorInfoProvider` that adds a date stamp to returned error objects:

```csharp
public class MyErrorInfoProvider : GraphQL.Execution.ErrorInfoProvider
{
    public override ErrorInfo GetInfo(ExecutionError executionError)
    {
        var info = base.GetInfo(executionError);
        info.Extensions["timestamp"] = DateTime.Now.ToString("u");
        return info;
    }
}
```
