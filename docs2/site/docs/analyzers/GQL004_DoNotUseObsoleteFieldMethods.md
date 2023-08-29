# GQL004: Don't use obsolete `Field` methods

## Cause

One of the deprecated `FieldXXX` methods that return `FieldType` were used.

## Rule description

A bunch of `FieldXXX`` APIs were deprecated and will be removed in future version.

## How to fix violations

You will need to change a way of setting fields on your graph types. Instead of many `FieldXXX` overloads, start configuring your field with one of the `Field` methods defined on `ComplexGraphType`. All such methods define a new field and return an instance of `FieldBuilder<T,U>`. Then continue to configure the field with rich APIs provided by the returned builder.

## Example of a violation

```c#
Field<NonNullGraphType<StringGraphType>>(
    "name",
    "Field description",
    resolve: context => context.Source!.Name);

FieldAsync<CharacterInterface>(
    "hero",
    resolve: async context => await data.GetDroidByIdAsync("3"));

FieldAsync<HumanType>(
    "human",
    arguments: new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>>
    {
        Name = "id",
        Description = "id of the human"
    }),
    resolve: async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id"))
);
```

## Example of how to fix

```c#
Field<NonNullGraphType<StringGraphType>>("name")
    .Description("Field description")
    .Resolve(context => context.Source!.Name);

Field<CharacterInterface>("hero")
    .ResolveAsync(async context => await data.GetDroidByIdAsync("3"));

Field<HumanType>("human")
    .Argument<NonNullGraphType<StringGraphType>>("id", "id of the human")
    .ResolveAsync(async context => await data.GetHumanByIdAsync(context.GetArgument<string>("id")));

```

## Related rules
