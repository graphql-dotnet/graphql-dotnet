# Migrating from v8 to v9

## ValueConverter Changes

### ValueConverter is now an instance class

`ValueConverter` has been changed from a static class to a non-static class. Each `ISchema` instance now has its own `ValueConverter` property.

**Before:**
```csharp
ValueConverter.Register<string, int>(value => int.Parse(value));
```

**After:**
```csharp
schema.ValueConverter.Register<string, int>(value => int.Parse(value));
```

### ToObject methods now require IValueConverter parameter

Methods like `ToObject`, `GetPropertyValue`, and `CompileToObject` now require an `IValueConverter` parameter.

**Before:**
```csharp
var obj = dictionary.ToObject(typeof(MyType), graphType);
```

**After:**
```csharp
var obj = dictionary.ToObject(typeof(MyType), graphType, schema.ValueConverter);
```

### Parser delegate signature changed

The `Parser` property on `FieldType` and `QueryArgument` now uses `Func<object, IValueConverter, object>?` instead of `Func<object, object>?`.

**Before:**
```csharp
fieldType.Parser = value => /* transform value */;
```

**After:**
```csharp
fieldType.Parser = (value, valueConverter) => /* transform value */;
```

**Note:** Backward compatibility overloads are provided in `FieldBuilder.ParseValue()` and `QueryArgumentExtensions.ParseValue()`.

### IFederationResolver.ParseRepresentation signature changed

**Before:**
```csharp
public object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation)
```

**After:**
```csharp
public object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation, IValueConverter valueConverter)
```

### ParserAttribute now supports both signatures

Parser methods referenced by `[Parser]` attribute can have either:
- `object MethodName(object value)` - old signature (still supported)
- `object MethodName(object value, IValueConverter valueConverter)` - new signature

### Benefits

- **Isolation**: Different schemas can have different conversion rules
- **Testability**: Easier to test with isolated converters
- **Thread-safety**: No shared static state between schemas
- **Flexibility**: Can customize value conversion per schema instance
