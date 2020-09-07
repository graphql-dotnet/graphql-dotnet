# Aliases

You can provide an alias for a queried field and rename it to anything you want.

```graphql
{
  empireHero: hero(id: "1") {
    nametest
  }
  jediHero: hero(id: "2") {
    name
  }
}
```
