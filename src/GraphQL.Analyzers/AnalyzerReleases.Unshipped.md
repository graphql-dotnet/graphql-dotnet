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
GQL006 | Usage | Warning | InputGraphTypeAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL006_CanNotMatchInputFieldToTheSourceField)
GQL007 | Usage | Warning | InputGraphTypeAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL007_CanNotSetSourceField)
GQL008 | Usage | Warning | FieldArgumentAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL008_DoNotUseObsoleteArgumentMethod)
GQL009 | Usage | Error | AwaitableResolverAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL009_UseAsyncResolver)
GQL011 | Usage | Error | NotAGraphTypeAnalyzer, [Documentation](https://graphql-dotnet.github.io/docs/analyzers/GQL011_MustNotBeConvertibleToGraphType)
