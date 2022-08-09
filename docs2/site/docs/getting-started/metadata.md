# Object/Field Metadata

Any `IGraphType`, `IFieldType`, `Directive`, `ISchema` and some other classes implement
the `IProvideMetadata` interface. This allows you to add arbitrary information to those objects.
This can be useful in combination with a validation rule or field middleware.

```csharp
public interface IProvideMetadata
{
  Dictionary<string, object> Metadata { get; }
  TType GetMetadata<TType>(string key, TType defaultValue = default);
  TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory);
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
