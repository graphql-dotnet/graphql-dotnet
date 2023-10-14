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
| `GlobalAttributes` | An empty collection |

For a detailed description of each option, see [GlobalSwitches](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/src/GraphQL/GlobalSwitches.cs).

# Global GraphQL Attributes

In addition to adding `GraphQLAttribute` instances to the collection noted above,
you may also apply `GraphQLAttribute`s globally by applying them to the module or assembly.
Code that utilizes `GraphQLAttribute`s, such as auto-registering graph types, will scan
the CLR type's owning module and assembly and apply any globally-defined attributes found.
Globally-defined attributes may be configured to execute before or after individually-specified
attributes by changing the `GraphQLAttribute.Priority` value. When writing your globally-defined
attribute do not forget to mark it with `AttributeUsage` attribute and include `AttributeTargets.Assembly`
or `AttributeTargets.Module`.
