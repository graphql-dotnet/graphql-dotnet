# GQL001: Define the name in `Field` method

## Cause

The `FieldBuilder` instance was created using the `Field` method overload that doesn't require a name argument, with the field name being supplied through the `Name` method.

## Rule description

The `Field` method overloads without name argument are obsolete and will be removed in future version.

## How to fix violations

Use the `Field` method overload that takes the name argument and remove the call to the `Name` method.

## Example of a violation

```c#
Field<StringGraphType>().Name("Name");
```

## Example of how to fix

```c#
Field<StringGraphType>("Name");
```

## Related rules

[GQL002: `Name` method invocation can be removed](/GQL002_NameMethodInvocationCanBeRemoved.md)  
[GQL003: Different names defined by `Field` and `Name` methods](/GQL003_DifferentNamesDefinedByFieldAndNameMethods.md)
