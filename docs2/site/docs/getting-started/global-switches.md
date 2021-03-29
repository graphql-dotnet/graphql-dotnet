# Global Switches

GraphQL.NET provides some global options for configuring GraphQL execution. These
options are called `GlobalSwitches`. These options are global, because they apply
immediately to all the schemas used in the application, therefore use them with caution.

The current options list is presented below:

1. `EnableReadDefaultValueFromAttributes`
2. `EnableReadDeprecationReasonFromAttributes`
3. `EnableReadDescriptionFromAttributes`
4. `EnableReadDescriptionFromXmlDocumentation`
5. `NameValidation`
6. `UseDeclaringTypeNames`

For a detailed description of each option, see [GlobalSwitches](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/GlobalSwitches.cs).
