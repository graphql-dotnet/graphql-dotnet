# Lists and Non-Null

Object types, scalars, and enums are the only kinds of types you can define in GraphQL.
But when you use the types in other parts of the schema, or in your query variable
declarations, you can apply additional _type modifiers_ that affect validation of those
values. Let's look at an example:

```graphql
type Character {
  name: String!
  appearsIn: [Episode]!
}
```

Here, we're using a `String` type and marking it as _Non-Null_ by adding an exclamation
mark, `!` after the type name. This means that our server always expects to return a
non-null value for this field, and if it ends up getting a null value that will actually
trigger a GraphQL execution error, letting the client know that something has gone wrong.

The Non-Null type modifier can also be used when defining arguments for a field, which
will cause the GraphQL server to return a validation error if a null value is passed as
that argument, whether in the GraphQL string or in the variables.

```graphql
query DroidById($id: ID!) {
  droid(id: $id) {
    name
  }
}
```

Lists work in a similar way: We can use a type modifier to mark a type as a `List`, which
indicates that this field will return an array of that type. In the schema language, this
is denoted by wrapping the type in square brackets, `[` and `]`. It works the same for
arguments, where the validation step will expect an array for that value.

The Non-Null and List modifiers can be combined. For example, you can have a List of Non-Null Strings:

```graphql
myField: [String!]
```

This means that the _list itself_ can be null, but it can't have any null members. For example, in JSON:

```js
myField: null // valid
myField: [] // valid
myField: ['a', 'b'] // valid
myField: ['a', null, 'b'] // error
```

Now, let's say we defined a Non-Null List of Strings:

```graphql
myField: [String]!
```

This means that the list itself cannot be null, but it can contain null values:

```js
myField: null // error
myField: [] // valid
myField: ['a', 'b'] // valid
myField: ['a', null, 'b'] // valid
```

You can arbitrarily nest any number of Non-Null and List modifiers, according to your needs.
