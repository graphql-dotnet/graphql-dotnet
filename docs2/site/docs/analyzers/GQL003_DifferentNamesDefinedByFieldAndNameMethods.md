# GQL003: Different names defined by `Field` and `Name` methods

## Cause

`Field` and `Name` methods define different field names.

## Rule description

You should supply the field name within the `Field` method. If you intend to use a different name, modify it within the `Field` method instead of using the `Name` method.

## How to fix violations

Specify the preferred field name within the `Field` method and remove the use of the `Name` method.

## Example of a violation

```c#
Field<StringGraphType>("Name1").Name("Name2");
```

## Example of how to fix

```c#
Field<StringGraphType>("Name1");
```

or

```c#
Field<StringGraphType>("Name2");
```

## Related rules

[GQL001: Define the name in `Field` method](/GQL001_DefineTheNameInFieldMethod.md)  
[GQL002: `Name` method invocation can be removed](/GQL002_NameMethodInvocationCanBeRemoved.md)
