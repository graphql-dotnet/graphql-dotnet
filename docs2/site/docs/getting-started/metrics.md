# Metrics

Metrics are captured during execution.  This can help you determine performance issues within a resolver or validation.  Field metrics are captured using Field Middleware and the results are returned as a `PerfRecord` array on the `ExecutionResult`.  You can then generate [Apollo Tracing](https://github.com/apollographql/apollo-tracing) data with the `EnrichWithApolloTracing()` extension method.

```csharp
var start = DateTime.UtcNow;

var executor = new DocumentExecutor();
ExecutionResult result = executor.ExecuteAsync(_ =>
{
  _.Schema = schema;
  _.Query = "...";
  _.EnableMetrics = true;
  _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
});

result.EnrichWithApolloTracing(start);
```

```json
{
  "data": {
    "hero": {
      "name": "R2-D2",
      "friends": [
        {
          "name": "Luke"
        },
        {
          "name": "C-3PO"
        }
      ]
    }
  },
  "extensions": {
    "tracing": {
      "version": 1,
      "startTime": "2018-07-28T21:39:27.160902Z",
      "endTime": "2018-07-28T21:39:27.372902Z",
      "duration": 212304000,
      "parsing": {
        "startOffset": 57436000,
        "duration": 21985999
      },
      "validation": {
        "startOffset": 57436000,
        "duration": 21985999
      },
      "execution": {
        "resolvers": [
          {
            "path": [
              "hero"
            ],
            "parentType": "Query",
            "fieldName": "hero",
            "returnType": "Character",
            "startOffset": 147389000,
            "duration": 2756000
          },
          {
            "path": [
              "hero",
              "name"
            ],
            "parentType": "Droid",
            "fieldName": "name",
            "returnType": "String",
            "startOffset": 208043000,
            "duration": 396000
          },
          {
            "path": [
              "hero",
              "friends"
            ],
            "parentType": "Droid",
            "fieldName": "friends",
            "returnType": "[Character]",
            "startOffset": 208533000,
            "duration": 1067999
          },
          {
            "path": [
              "hero",
              "friends",
              0,
              "name"
            ],
            "parentType": "Human",
            "fieldName": "name",
            "returnType": "String",
            "startOffset": 210501000,
            "duration": 33999
          },
          {
            "path": [
              "hero",
              "friends",
              1,
              "name"
            ],
            "parentType": "Droid",
            "fieldName": "name",
            "returnType": "String",
            "startOffset": 210542000,
            "duration": 3000
          }
        ]
      }
    }
  }
}
```
