# GQL005: Illegal resolver usage

## Cause

A `ResolveXXX` method was invoked for the field that is defined within the non-output graph type.

## Rule description

Resolvers are only allowed on the output graph types. Output graph types are types derived from the `ObjectGraphType` class or implementing `IObjectGraphType` interface.

## How to fix violations

Remove the `ResolveXXX` method call.

## Example of a violation

The following example shows `Resolve` and `ResolveAsync` methods used on the fields defined within input and interface graph types.

```c#
public class MyInputGraphType : InputObjectGraphType<User>
{
    public MyInputGraphType() =>
        Field<StringGraphType>("Name").Resolve(context => context.Source.Name);
}

public class MyInterfaceGraphType : InterfaceGraphType<Person>
{
    public MyInterfaceGraphType(IStore store) =>
        Field<ListGraphType<NonNullGraphType<PersonGraphType>>>("Children")
            .ResolveAsync(async context => await store.GetChildrenAsync(context.Source.Name));
}
```

## Example of how to fix

```c#
public class MyInputGraphType : InputObjectGraphType<User>
{
    public MyInputGraphType() =>
        Field<StringGraphType>("Name");
}

public class MyInterfaceGraphType : InterfaceGraphType<Person>
{
    public MyInterfaceGraphType(IStore store) =>
        Field<ListGraphType<NonNullGraphType<PersonGraphType>>>("Children");
}
```

## Related rules
