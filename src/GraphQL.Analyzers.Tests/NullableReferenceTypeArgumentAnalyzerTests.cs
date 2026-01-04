using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.NullableReferenceTypeArgumentAnalyzer,
    GraphQL.Analyzers.NullableReferenceTypeArgumentCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class NullableReferenceTypeArgumentAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("\"arg\"", "\"arg\", nullable: true")]
    [InlineData("\"arg\", null", "\"arg\", true")]
    [InlineData("\"arg\", default", "\"arg\", true")]
    [InlineData("\"arg\", nullable: null", "\"arg\", nullable: true")]
    [InlineData("\"arg\", null, \"description\"", "\"arg\", true, \"description\"")]
    [InlineData("\"arg\", description: \"description\", nullable: null", "\"arg\", description: \"description\", nullable: true")]
    [InlineData("\"arg\", null, arg => { }", "\"arg\", true, arg => { }")]
    public async Task NullableReferenceType_InvalidNullableParameter_ReportsDiagnostic(string arguments, string expectedArguments)
    {
        string source =
            $$"""
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<{|#0:string?|}>({{arguments}});
                }
            }
            """;

        string fix =
            $$"""
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>({{expectedArguments}});
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData("nullable: true")]
    [InlineData("nullable: false")]
    public async Task NullableReferenceType_ValidNullableParameter_NoDiagnostic(string nullableArg)
    {
        string source =
            $$"""
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", {{nullableArg}});
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterVariable_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    bool? isNullable = GetNullable();
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", isNullable);
                }

                private bool? GetNullable() => null;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NonNullableReferenceType_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string>("arg");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullableValueType_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<int?>("arg");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NonNullableValueType_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<int>("arg");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GenericTypeParameter_NoDiagnostic()
    {
        const string source =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType<T> : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<T>("arg");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullableDisabled_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string>("arg");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
