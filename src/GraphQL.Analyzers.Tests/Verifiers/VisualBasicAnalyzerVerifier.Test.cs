using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace GraphQL.Analyzers.Tests.Verifiers;

public static partial class VisualBasicAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public class Test : VisualBasicAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        public Test()
        {
        }
    }
}
