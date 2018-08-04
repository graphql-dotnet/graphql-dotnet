# Fragments

Fragments let you construct sets of fields and then include them in queries where you need to.

```graphql
query {
  leftComparison: hero(id: "1") {
    ...comparisonFields
  }
  rightComparison: hero(id: "2") {
    ...comparisonFields
  }
}

fragment comparisonFields on Character {
  name
  appearsIn
  friends {
    name
  }
}
```
