# GraphQL.NET Analyzer Documentation

This documentation provides an overview of the analyzers that have been included
in GraphQL.NET and how to configure them using the `.editorconfig` file.

## Introduction

Analyzers in GraphQL.NET are tools that help you identify and address potential
issues or improvements in your GraphQL code. This documentation covers currently
available analyzers and code fixes, along with the methods to configure them.

## Configuration in .editorconfig

Certain analyzers and code fixes offer configuration options that control when
the rule is applied and how the automatic code fix executes code adjustments.
Refer to the specific documentation page for each analyzer to understand the
available configuration options and their application methods.

### Set rule severity

You can set the severity for analyzer rules in `.editorconfig` file with the
following syntax:

```ini
dotnet_diagnostic.<rule ID>.severity = <severity>
```

For example

```ini
dotnet_diagnostic.GQL001.severity = none
```

> Note: configuration keys are case insensitive

For more information about analyzers configuration see
[Code Analysis Configuration Overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022)

## Included Analyzers

| Identifier          | Name                                                                                              |
| ------------------- | ------------------------------------------------------------------------------------------------- |
| [GQL001](../gql001) | Define the name in Field, `Connection` or `ConnectionBuilder.Create` method                       |
| [GQL002](../gql002) | `Name` method invocation can be removed                                                           |
| [GQL003](../gql003) | Different names defined by `Field`, `Connection` or `ConnectionBuilder.Create` and `Name` methods |
| [GQL004](../gql004) | Don't use obsolete `Field` methods                                                                |
| [GQL005](../gql005) | Illegal resolver usage                                                                            |
| [GQL006](../gql006) | Can not match input field to the source field                                                     |
| [GQL007](../gql007) | Can not set source field                                                                          |
| [GQL008](../gql008) | Don't use an obsolete `Argument` method                                                           |
| [GQL009](../gql009) | Use async resolver                                                                                |
| [GQL010](../gql010) | Can not resolve input source type constructor                                                     |
| [GQL011](../gql011) | The type must not be convertible to `IGraphType`                                                  |
| [GQL012](../gql012) | Illegal method usage                                                                              |
| [GQL013](../gql013) | `OneOf` fields must be nullable                                                                   |
| [GQL014](../gql014) | `OneOf` fields must not have default value                                                        |
| [GQL015](../gql015) | Can't infer a `Field` name from expression                                                        |
| [GQL016](../gql016) | Require parameterless constructor                                                                 |
| [GQL017](../gql017) | Could not find method                                                                             |
| [GQL018](../gql018) | `Parser` method must be valid                                                                     |
| [GQL019](../gql019) | `Validator` method must be valid                                                                  |
| [GQL020](../gql020) | `ValidateArguments` method must be valid                                                          |
| [GQL021](../gql021) | Nullable reference type argument should specify nullable parameter                                |
| [GQL022](../gql022) | AOT schema attributes must be applied to classes deriving from `AotSchema`                        |

## Federation Analyzers

| Identifier                | Name                                             |
| ------------------------- | ------------------------------------------------ |
| [GQLFED001](../gqlfed001) | Key field does not exist                         |
| [GQLFED002](../gqlfed002) | Key must not be null or empty                    |
| [GQLFED003](../gqlfed003) | Duplicate key                                    |
| [GQLFED004](../gqlfed004) | Redundant key                                    |
| [GQLFED005](../gqlfed005) | Key field must not have arguments                |
| [GQLFED006](../gqlfed006) | Key field must not be an interface or union type |
