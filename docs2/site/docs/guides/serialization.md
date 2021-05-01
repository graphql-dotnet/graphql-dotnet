# JSON Serialization and Deserialization

Two libraries are available for assistance deserializing JSON-formatted GraphQL requests,
and serializing GraphQL responses in a JSON format. It is not mandatory to use JSON for the
request or response format, but it is common to do so. The two libraries are:

* [GraphQL.SystemTextJson](https://www.nuget.org/packages/GraphQL.SystemTextJson), for use with the `System.Text.Json` library, and
* [GraphQL.NewtonsoftJson](https://www.nuget.org/packages/GraphQL.NewtonsoftJson), for use with the `Newtonsoft.Json` library

These two projects have very similar classes and extension methods available. There are two notable
differences between the two libraries. First, the `Newtonsoft.Json` library does not provide asynchronous
serialization or deserialization methods. Due to this reason, the async `GraphQL.NewtonsoftJson` serialization
helper actually performs synchronous calls on the underlying stream when writing the JSON output. This is
significant when hosting the service via ASP.NET Core, as it is required to specifically allow synchronous
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

You can also configure the serialization and deserialization process to your needs
with either library, such as using case insensitive matching with `System.Text.Json`
or enabling or disabling indenting on the serialized output. There are a number of
other differences as well.
[Click this link](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-5-0#table-of-differences-between-newtonsoftjson-and-systemtextjson)
for a comprehensive table of differences between the two serialization engines.

The remainder of the documentation here will assume the use of the `GraphQL.SystemTextJson`
library; slight changes may be necessary if you are using the `GraphQL.NewtonsoftJson` library.

# Serialization

Serialization of a `ExecutionResult` node is handled by `ExecutionResultJsonConverter` which
accepts in its constructor an instance of `IErrorInfoProvider` (see below). The converter can
be registered within an instance of `JsonSerializerOptions` so that serializing an `ExecutionResult`
produces the proper output.

To assist, a `DocumentWriter` class is provided with a single method, `WriteAsync`, which
handles constructing the options, registering the converter, and serializing a specified
`ExecutionResult` to a data stream. This class is designed to be registered as a singleton
within your dependency injection framework, if applicable.

Here is an example of its use within the `Harness` sample project:

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

# Deserialization

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
public static ValueTask<T> FromJsonAsync<T>(this System.IO.Stream stream, CancellationToken cancellationToken = default);
```

Here are a couple typical examples:

```csharp
// ASP.Net Core action with multipart/form-data or application/x-www-form-urlencoded encoding
[HttpPost("graphql")]
public IActionResult GraphQL(string query, string operationName, string variables)
{
    var inputs = variables?.ToInputs();
    ...
}


// ASP.Net Core action with json encoding
[HttpPost("graphql")]
public IActionResult GraphQL([FromBody] Request request)
{
    ...
}

public class Request
{
    string Query { get; set; }
    string OperationName { get; set; }
    JsonElement Variables { get; set; }
}


// Other, with the request as a JSON string
var request = requestString.FromJson<Request>();

public class Request
{
    string Query { get; set; }
    string OperationName { get; set; }
    Inputs Variables { get; set; }
}
```

# ErrorInfoProvider
