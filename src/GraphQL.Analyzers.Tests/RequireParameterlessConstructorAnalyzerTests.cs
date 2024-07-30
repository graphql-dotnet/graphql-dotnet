using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.RequireParameterlessConstructorAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class RequireParameterlessConstructorAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, null, false)]
    [InlineData(2, "public TestFieldComplexityAnalyzer() {}", false)]
    [InlineData(3, "private TestFieldComplexityAnalyzer() {}", true)]
    [InlineData(4, "protected TestFieldComplexityAnalyzer() {}", true)]
    [InlineData(5, "internal TestFieldComplexityAnalyzer() {}", true)]
    [InlineData(6, "public TestFieldComplexityAnalyzer(int i) {}", true)]
    public async Task Constructors_GQL016(int idx, string? constructor, bool report)
    {
        _ = idx;
        string source =
            $$"""
            using GraphQL.Validation.Complexity;

            namespace Sample.Server;

            public class {|#0:TestFieldComplexityAnalyzer|} : IFieldComplexityAnalyzer
            {
                {{constructor}}

                public FieldComplexityResult Analyze(FieldImpactContext context) =>
                    throw new System.NotImplementedException();
            }
            """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(RequireParameterlessConstructorAnalyzer.RequireParameterlessConstructor)
                    .WithLocation(0).WithArguments("TestFieldComplexityAnalyzer")
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "()", false)]
    [InlineData(2, "(int i)", true)]
    public async Task PrimaryConstructors_GQL016(int idx, string? constructor, bool report)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL.Validation.Complexity;

              namespace Sample.Server;

              public class {|#0:TestFieldComplexityAnalyzer|}{{constructor}} : IFieldComplexityAnalyzer
              {
                  public FieldComplexityResult Analyze(FieldImpactContext context) =>
                      throw new System.NotImplementedException();
              }
              """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(RequireParameterlessConstructorAnalyzer.RequireParameterlessConstructor)
                    .WithLocation(0).WithArguments("TestFieldComplexityAnalyzer")
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, null, false)]
    [InlineData(2, "public TestGraphType() {}", false)]
    [InlineData(3, "private TestGraphType() {}", true)]
    [InlineData(4, "protected TestGraphType() {}", true)]
    [InlineData(5, "internal TestGraphType() {}", true)]
    [InlineData(6, "public TestGraphType(int i) {}", true)]
    public async Task GenericParameter_GQL016(int idx, string? constructor, bool report)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class Test
              {
                  public IGraphTypeFactory<TestGraphType> CreateFactory() =>
                      new DefaultGraphTypeFactory<{|#0:TestGraphType|}>();
              }

              public class TestGraphType : ObjectGraphType
              {
                  {{constructor}}
              }
              """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(RequireParameterlessConstructorAnalyzer.RequireParameterlessConstructor)
                    .WithLocation(0).WithArguments("TestGraphType")
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
