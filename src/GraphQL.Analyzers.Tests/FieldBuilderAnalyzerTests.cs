using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.FieldBuilderAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class FieldBuilderAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    // good
    [InlineData(10, null, "p => p.FirstName", false)]
    [InlineData(11, "Name", "p => p.FirstName", false)]
    [InlineData(12, "FullName", "p => $\"{p.FirstName} {p.LastName}\"", false)]
    [InlineData(13, "FullName", "p => p.FirstName + \" \" + p.LastName", false)]
    // bad
    [InlineData(14, null, "p => $\"{p.FirstName} {p.LastName}\"", true)]
    [InlineData(15, null, "p => p.FirstName + \" \" + p.LastName", true)]
    public async Task InferFieldNameFromExpression_GQL015(int idx, string? name, string expression, bool report)
    {
        _ = idx;

        string? nameArg = name == null ? null : $"\"{name}\", ";
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class PersonGraphType : ObjectGraphType<Person>
              {
                  public PersonGraphType()
                  {
                      Field({{nameArg}}{|#0:{{expression}}|});
                  }
              }

              public class Person
              {
                  public string FirstName { get; set; }
                  public string LastName { get; set; }
              }
              """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(FieldBuilderAnalyzer.CantInferFieldNameFromExpression)
                    .WithLocation(0).WithArguments(expression)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
