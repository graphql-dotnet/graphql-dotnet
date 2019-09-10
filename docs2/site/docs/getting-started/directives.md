# Directives

A directive can be attached to almost every part of the schema - field, query, enum, fragment inclusion etc. and can affect execution of the query in any way the server desires. The core GraphQL specification includes exactly three directives.

* `@include(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Only include this field in the result if the argument is true.
* `@skip(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT` Skip this field if the argument is true.
* `@deprecated(reason: String = "No longer supported") on FIELD_DEFINITION | ENUM_VALUE` Indicates deprecated portions of a GraphQL service’s schema, such as deprecated fields on a type or deprecated enum values.

```graphql
query HeroQuery($id: ID, $withFriends: Boolean!) {
  hero(id: $id) {
    name
    friends @include(if: $withFriends) {
      name
    }
  }
}
```
