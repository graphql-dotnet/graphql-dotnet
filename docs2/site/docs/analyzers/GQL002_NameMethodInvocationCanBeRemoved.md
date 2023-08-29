# GQL002: `Name` method invocation can be removed

## Cause

The same name is provided in `Field` and `Name` methods.

## Rule description

Field name should be provided in the `Field` method. The `Name` method call is unnecessary and can be removed.

## How to fix violations

Remove the `Name` method call.

## Example of a violation

```c#
Field<StringGraphType>("Name").Name("Name");
```

## Example of how to fix

```c#
Field<StringGraphType>("Name");
```

## Related rules

[GQL001: Define the name in `Field` method](/GQL001_DefineTheNameInFieldMethod.md)  
[GQL003: Different names defined by `Field` and `Name` methods](/GQL003_DifferentNamesDefinedByFieldAndNameMethods.md)
