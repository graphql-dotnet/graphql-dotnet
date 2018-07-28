# Directives

A directive can be attached to a field or fragment inclusion and can affect execution of the query in any way the server desires. The core GraphQL specification includes exactly two directives.

* `@include(if: Boolean)` Only include this field in the result if the argument is true.
* `@skip(if: Boolean)` Skip this field if the argument is true.

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
