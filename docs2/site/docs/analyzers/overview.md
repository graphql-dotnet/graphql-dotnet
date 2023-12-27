# GraphQL.NET Analyzer Documentation

This documentation provides an overview of the analyzers that have been included
in GraphQL.NET and how to configure them using the `.editorconfig` file.

## Introduction

Analyzers in GraphQL.NET are tools that help you identify and address potential
issues or improvements in your GraphQL code. This documentation covers currently
available analyzers and code fixes, along with the methods to configure them.

## Included Analyzers

### 1. FieldBuilderAnalyzer

This analyzer focuses on replacing obsolete methods involving field building. It
aims to enhance code clarity and consistency by updating the way fields are
defined. The primary transformation performed by this analyzer is as follows:

Replace:

```csharp
Field(name: "name", description..., resolver: ...)
```

With:

```csharp
Field("name").Description(...).Resolve(...)
```

### 2. FieldNameAnalyzer

The `FieldNameAnalyzer` detects the usage of the obsolete `Name` method on the
field and connection builders and helps to rewrite the code to use builder
creating methods that accept the name as an argument.

Replace:

```csharp
Field<TGraphType>().Name("name")
Connection<TGraphType, TSourceType>().Name("name")
```

With:

```csharp
Field<TGraphType>("name")
Connection<TGraphType, TSourceType>("name")
```

### 3. ResolverAnalyzer

The `ResolverAnalyzer` focuses on identifying incorrect usage of resolver
methods. It ensures that resolver methods are used only on output graph types
and flags usage on other types as errors. Additionally, it offers a code fix
that removes these incorrect usages.

### 4. InputGraphTypeAnalyzer

The analyzer detects input graph type fields that can't be mapped to the source
type during deserialization process.

### 5. FieldArgumentAnalyzer

The analyzer detects an obsolete `Argument` method usage and offers a code fix
to automatically replace it with with another `Argument` overload.

### 6. AwaitableResolverAnalyzer

The analyzer detects awaitable resolver delegates used in sync `Resolve` or
`ResolveScoped` methods and provides a code fix to replace them with an
appropriate async version.

### 7. NotAGraphTypeAnalyzer

The analyzer identifies instances of incorrectly using `GraphType` as generic
type argument, where the type argument should not be of type `IGraphType`.

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
