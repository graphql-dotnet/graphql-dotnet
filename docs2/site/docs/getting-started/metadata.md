# Object/Field Metadata

`IGraphType` and `FieldType` implement the `IProvideMetadata` interface. This allows you to add arbitrary information to a field or graph type. This can be useful in combination with a validation rule or field middleware.

```csharp
public interface IProvideMetadata
{
  IDictionary<string, object> Metadata { get; }
  TType GetMetadata<TType>(string key, TType defaultValue = default(TType));
  bool HasMetadata(string key);
}
```

```csharp
public class MyGraphType : ObjectGraphType
{
  public MyGraphType()
  {
    Metadata["rule"] = "value";
  }
}
```
