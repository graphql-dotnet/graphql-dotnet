using GraphQL.Analyzers.Tests.Verifiers.XUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace GraphQL.Analyzers.Tests.Verifiers;

public static partial class VisualBasicAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public class Test : VisualBasicAnalyzerTest<TAnalyzer, XUnitVerifier>
    {
        public Test()
        {
        }
    }
}
