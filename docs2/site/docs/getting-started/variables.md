# Variables

You can pass variables received from the client to the execution engine by using the `Variables` property.

> See the [official GraphQL documentation on variables](http://graphql.org/learn/queries/#variables)

Here is what a query looks like with a variable:

```graphql
query DroidQuery($droidId: String!) {
  droid(id: $droidId) {
    id
    name
  }
}
```

Here is what this query would look like as a JSON request:

```json
{
 "query": "query DroidQuery($droidId: String!) { droid(id: $droidId) { id name } }",
 "variables": {
   "droidId": "1"
 }
}
```

Call `.Deserialize<GraphQLRequest>()` to parse a JSON request to provide it to the `DocumentExecuter`:

```csharp
var requestJson = /* request as shown above */;
var request = new GraphQLSerializer().Deserialize<GraphQLRequest>(requestJson);

var result = await schema.ExecuteAsync(options =>
{
  options.Query = request.Query;
  options.OperationName = request.OperationName;
  options.Variables = request.Variables;
  options.Extensions = request.Extensions;
});
```

If you need to parse the variables separately from the query, you can call `.Deserialize<Inputs>()` to parse
a JSON-formatted variables string to an `Inputs` class suitable for passing to the `DocumentExecuter`:

```csharp
var variablesJson = /* get from request */;
var inputs = new GraphQLSerializer().Deserialize<Inputs>(variablesJson);

await schema.ExecuteAsync(options =>
{
  options.Query = "...";
  options.Variables = inputs;
});
```

Please note that you will need either the `GraphQL.SystemTextJson` or `GraphQL.NewtonsoftJson` nuget package
to run the above code, with the appropriate `using` statement.

When using dependency injection, you will typically register the serializer via `.AddSystemTextJson()` or
`.AddNewtonsoftJson()` in your DI configuration code, and then pull in instances of `IGraphQLTextSerializer`,
`ISchema` and `IDocumentExecuter`, resulting with code more similar to the following:

```csharp
Task<string> ExecuteAsync(string request, CancellationToken cancellationToken = default)
{
  var request = _serializer.Deserialize<GraphQLRequest>(request);
  var result = await _documentExecuter.ExecuteAsync(options =>
  {
    options.Schema = _schema;
    options.Query = request.Query;
    options.OperationName = request.OperationName;
    options.Variables = request.Variables;
    options.Extensions = request.Extensions;
    options.CancellationToken = cancellationToken;
  });
  var response = _serializer.Serialize(result);
  return response;
}
```

You can also use the `.Read<T>()` and `.Write()` methods of the serializer for `Stream`-based asynchronous
serialization and deserialization.
