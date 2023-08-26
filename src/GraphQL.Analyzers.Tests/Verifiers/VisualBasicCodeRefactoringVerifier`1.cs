using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

namespace GraphQL.Analyzers.Tests.Verifiers;

public static partial class VisualBasicCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, string)"/>
    public static async Task VerifyRefactoringAsync(string source, string fixedSource)
    {
        await VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult, string)"/>
    public static async Task VerifyRefactoringAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        await VerifyRefactoringAsync(source, new[] { expected }, fixedSource).ConfigureAwait(false);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult[], string)"/>
    public static async Task VerifyRefactoringAsync(string source, DiagnosticResult[] expected, string fixedSource)
    {
        var test = new Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
