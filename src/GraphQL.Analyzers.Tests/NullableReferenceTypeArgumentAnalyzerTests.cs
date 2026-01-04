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

    [Fact]
    public async Task NullableReferenceType_NoNullableParameter_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg");
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", nullable: true);
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterNull_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg", null);
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", true);
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterDefault_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg", default);
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", true);
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterNullNamed_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg", nullable: null);
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", nullable: true);
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_WithDescription_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg", null, "description");
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", true, "description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_WithConfigure_ReportsDiagnostic()
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
                        .Argument<{|#0:string?|}>("arg", null, arg => { });
                }
            }
            """;

        const string fix =
            """
            #nullable enable
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test")
                        .Argument<string?>("arg", true, arg => { });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("string?");
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterTrue_NoDiagnostic()
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
                        .Argument<string?>("arg", nullable: true);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NullableReferenceType_NullableParameterFalse_NoDiagnostic()
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
                        .Argument<string?>("arg", nullable: false);
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
