# GraphQL.NET Analyzer Documentation

This documentation provides an overview of the analyzers that have been included in GraphQL.NET and how to configure them using the `.editorconfig` file.

## Introduction

Analyzers in GraphQL.NET are tools that help you identify and address potential issues or improvements in your GraphQL code. This documentation covers currently available analyzers and code fixes, along with the methods to configure them.

## Included Analyzers

### 1. FieldBuilderAnalyzer

This analyzer focuses on replacing obsolete methods involving field building. It aims to enhance code clarity and consistency by updating the way fields are defined. The primary transformation performed by this analyzer is as follows:

Replace:

```csharp
Field(name: "name", description..., resolver: ...)
```

With:

```csharp
Field("name").Description(...).Resolve(...)
```

### 2. FieldNameAnalyzer

The `FieldNameAnalyzer` is designed to improve the field name definition method. It simplifies the way field names are assigned to enhance code consistency.

Replace:

```csharp
Field().Name("name")
```

With:

```csharp
Field("name")
```

### 3. ResolverAnalyzer

The `ResolverAnalyzer` focuses on identifying incorrect usage of resolver methods. It ensures that resolver methods are used only on output graph types and flags usage on other types as errors. Additionally, it offers a code fix that removes these incorrect usages.

## Configuration in .editorconfig

Certain analyzers and code fixes offer configuration options that control when the rule is applied and how the automatic code fix executes code adjustments. Refer to the specific documentation page for each analyzer to understand the available configuration options and their application methods.

### Set rule severity

You can set the severity for analyzer rules in `.editorconfig` file with the following syntax:

```ini
dotnet_diagnostic.<rule ID>.severity = <severity>
```

For example

```ini
dotnet_diagnostic.GQL001.severity = none
```

You can set the severity for all rules in a specific category with the following syntax (**TODO: currently doesn't work**):

```ini
dotnet_analyzer_diagnostic.category-<rule category>.severity = <severity>
```

For example

```ini
dotnet_analyzer_diagnostic.category-deprecations.severity = error
```

> Note: configuration keys are case insensitive

For more information about analyzers configuration see [Code Analysis Configuration Overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2022)
