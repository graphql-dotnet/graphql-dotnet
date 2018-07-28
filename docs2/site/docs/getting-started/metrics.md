# Metrics

Metrics are captured during execution.  This can help you determine performance issues within a resolver or validation.  Field metrics are captured using Field Middleware and the results are returned as a `PerfRecord` array on the `ExecutionResult`.  You can then generate a report from those records using `StatsReport`.

```csharp
var start = DateTime.UtcNow;

var result = schema.Execute(_ =>
{
    _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
});

var report = StatsReport.From(schema, result.Operation, result.Perf, start);
```
