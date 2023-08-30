# GraphQL.NET Analyzer Documentation

This documentation provides an overview of the analyzers that have been included in GraphQL.NET and how to configure them using the `.editorconfig` file.

## Introduction

Analyzers in GraphQL.NET are tools that help you identify and address potential issues or improvements in your GraphQL code. This documentation covers the three main analyzers that have been introduced in the recent update.

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

The `FieldNameAnalyzer` is designed to improve the naming conventions for field definitions. It simplifies the way field names are assigned to enhance code readability.

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

You can configure the behavior of these analyzers using the `.editorconfig` file in your project. Two configuration keys are available for customization:

- `dotnet_diagnostic.GQL004.reformat`: This key controls whether the code should be reformatted based on the analyzer's suggestions. Set it to `true` to enable reformatting.
- `dotnet_diagnostic.GQL004.skip_nulls`: This key controls whether null values should be skipped during the code transformation. Set it to `true` to skip null values.

Example `.editorconfig` configuration:

```ini
# .editorconfig

dotnet_diagnostic.GQL004.reformat = true
dotnet_diagnostic.GQL004.skip_nulls = true
```

With these configuration settings, the analyzers will perform code transformations according to the specified preferences. This helps maintain consistent formatting and reduces unnecessary null values in the code.

## Conclusion

GraphQL.NET analyzers are powerful tools that can enhance your GraphQL codebase by identifying and addressing issues related to field building, naming conventions, and resolver usage. By configuring these analyzers in your project's `.editorconfig`, you can ensure that your code remains clean, readable, and aligned with best practices.
