# JSON Serialization and Deserialization

Two libraries are available for assistance deserializing JSON-formatted GraphQL requests,
and serializing responses in a JSON format. It is not mandatory to use JSON for the request
or response format, but it is common to do so. The two libraries are:

* [GraphQL.SystemTextJson](https://www.nuget.org/packages/GraphQL.SystemTextJson), for use with the `System.Text.Json` library, and
* [GraphQL.NewtonsoftJson](https://www.nuget.org/packages/GraphQL.NewtonsoftJson), for use with the `Newtonsoft.Json` library

These two projects have very similar classes and extension methods available. There are two notable
differences between the two libraries. First, the `Newtonsoft.Json` library does not provide asynchronous
serialization or deserialization methods. Due to this reason, the async `GraphQL.NewtonsoftJson` serialization
helper actually performs synchronous calls on the underlying stream when writing the JSON output. This is
significant when hosting the service via ASP.NET Core, as it is required to specifically allow synchronous
reading and writing of the underlying stream.

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

# Serialization

# Deserialization
