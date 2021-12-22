# Variables

You can pass variables received from the client to the execution engine by using the `Inputs` property.

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

Call `.ToInputs()` to translate JSON variables into a format that the library can work with.

```csharp
var variablesJson = // get from request
// `ToInputs` extension method converts the json to the `Inputs` class
var inputs = variablesJson.ToInputs();

await schema.ExecuteAsync(_ =>
{
  _.Query = "...";
  _.Variables = inputs;
});
```
