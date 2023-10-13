using GraphQL.Analyzers.Tests.Verifiers.XUnit;
using VerifyCS = GraphQL.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class XUnitTests
{
    [Fact]
    public async Task VerifyProperExceptionReturned()
    {
        // see https://github.com/dotnet/roslyn-sdk/issues/1099
        // problem exists when using xunit 2.5.0 with Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit 1.1.1

        const string source = """
            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
            }
            """;

        // error CS0246: The type or namespace name 'ObjectGraphType' could not be found (are you missing a using directive or an assembly reference?)
        await Assert
            .ThrowsAsync<EqualWithMessageException>(async () => await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false))
            .ConfigureAwait(false);
    }
}
