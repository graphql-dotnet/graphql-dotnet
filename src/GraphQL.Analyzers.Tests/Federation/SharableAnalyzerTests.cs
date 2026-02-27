using GraphQL.Analyzers.Federation;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.Federation.SharableAnalyzer>;

namespace GraphQL.Analyzers.Tests.Federation;

public class SharableAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(".Shareable()")]
    [InlineData(null)]
    public async Task ObjectType_NoDiagnostics(string? directive)
    {
        string source =
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample.Server;

            public class ProductGraphType : ObjectGraphType
            {
                public ProductGraphType()
                {
                    Field<StringGraphType>("name"){{directive}};
                    Field<StringGraphType>("description"){{directive}};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("InterfaceGraphType", "interface", ".{|#0:Shareable()|}", true)]
    [InlineData("InputObjectGraphType", "input", ".{|#0:Shareable()|}", true)]
    [InlineData("InterfaceGraphType", "interface", null, false)]
    [InlineData("InputObjectGraphType", "input", null, false)]
    public async Task NonObject_ReportWhenSharableField(string parentType, string type, string? directive, bool report)
    {
        string source =
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample.Server;

            public class Product{{parentType}} : {{parentType}}
            {
                public Product{{parentType}}()
                {
                    Field<StringGraphType>("name"){{directive}};
                }
            }

            public class Product
            {
                public string Name { get; set; }
            }
            """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(SharableAnalyzer.ShareableNotAllowedOnInterface)
                    .WithLocation(0)
                    .WithArguments("name", type, $"Product{parentType}")
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("InterfaceGraphType", "interface")]
    [InlineData("InputObjectGraphType", "input")]
    public async Task NonObject_WithMultipleShareableFields_ReportAllSharableField(string parentType, string type)
    {
        string source =
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample.Server;

            public class Product{{parentType}} : {{parentType}}
            {
                public Product{{parentType}}()
                {
                    Field<IdGraphType>("id");
                    Field<StringGraphType>("name").{|#0:Shareable()|};
                    Field<StringGraphType>("description").{|#1:Shareable()|};
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic(SharableAnalyzer.ShareableNotAllowedOnInterface)
                .WithLocation(0)
                .WithArguments("name", type, $"Product{parentType}"),
            VerifyCS.Diagnostic(SharableAnalyzer.ShareableNotAllowedOnInterface)
                .WithLocation(1)
                .WithArguments("description", type, $"Product{parentType}")
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
