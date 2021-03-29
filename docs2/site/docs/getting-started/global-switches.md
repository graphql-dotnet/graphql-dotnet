# Global Switches

GraphQL.NET provides some global options for configuring GraphQL execution. These
options are called `GlobalSwitches`. These options are global, because they apply
immediately to all the schemas used in the application, therefore use them with caution.
It is recommended to set these options at the very beginning of the application before
performing any other code from GraphQL.NET.

The current options list is presented below:

| Property Name | Default Value |
|---------------|---------------|
| `EnableReadDefaultValueFromAttributes` | `true` |
| `EnableReadDeprecationReasonFromAttributes` | `true`
| `EnableReadDescriptionFromAttributes` | `true`
| `EnableReadDescriptionFromXmlDocumentation` | `false` |
| `NameValidation` | `NameValidator.ValidateDefault` |
| `UseDeclaringTypeNames` | `false` |

For a detailed description of each option, see [GlobalSwitches](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/GlobalSwitches.cs).
