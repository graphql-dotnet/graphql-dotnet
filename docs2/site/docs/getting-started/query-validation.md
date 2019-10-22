# Query Validation

There [are a number of query validation rules](http://facebook.github.io/graphql/June2018/#sec-Validation) that are ran when a query is executed.  All of these are turned on by default.  You can add your own validation rules or clear out the existing ones by setting the `ValidationRules` property.

```csharp
schema.Execute(_ =>
{
  _.Query = "...";
  _.ValidationRules =
    new[]
    {
      new RequiresAuthValidationRule()
    }
    .Concat(DocumentValidator.CoreRules());
});
```
