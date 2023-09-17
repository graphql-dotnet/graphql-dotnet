; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
GQL001 | Usage | Warning | FieldNameAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL001_DefineTheNameInFieldMethod)
GQL002 | Usage | Warning | FieldNameAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL002_NameMethodInvocationCanBeRemoved)
GQL003 | Usage | Warning | FieldNameAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL003_DifferentNamesDefinedByFieldAndNameMethods)
GQL004 | Usage | Warning | FieldBuilderAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL004_DoNotUseObsoleteFieldMethods)
GQL005 | Usage | Error | ResolverAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL005_IllegalResolverUsage)
