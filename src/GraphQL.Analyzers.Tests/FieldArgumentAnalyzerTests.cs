using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.FieldArgumentAnalyzer,
    GraphQL.Analyzers.FieldArgumentCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class FieldArgumentAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Argument_WithSingleTypeParameter_NoDiagnostic()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Test").Argument<StringGraphType>("arg", "description");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Argument_WithoutDefaultValueArgument_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").{|#0:Argument<StringGraphType, string>("arg", "description")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").Argument<StringGraphType>("arg", "description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithoutDefaultValueArgument2_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").{|#0:Argument<StringGraphType, string>("arg", "description", configure: argument => argument.DefaultValue = "MyDefault")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").Argument<StringGraphType>("arg", "description", configure: argument => argument.DefaultValue = "MyDefault");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithoutConfigureFunc_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>("arg", "description", "MyDefault")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>("arg", "description", argument => argument.DefaultValue = "MyDefault");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithoutConfigureFunc2_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>(
                            "arg",
                            "description",
                            "MyDefault")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>(
                            "arg",
                            "description",
                            argument => argument.DefaultValue = "MyDefault");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>("arg", "description", "MyDefault", argument => argument.DeprecationReason = "Deprecation Reason")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>("arg", "description", argument => { argument.DeprecationReason = "Deprecation Reason"; argument.DefaultValue = "MyDefault"; });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc2_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>("arg", "description", "MyDefault", argument =>
                            argument.DeprecationReason = "Deprecation Reason")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>("arg", "description", argument =>
                            {
                                argument.DeprecationReason = "Deprecation Reason";
                                argument.DefaultValue = "MyDefault";
                            });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc3_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>(
                            "arg",
                            "description",
                            "MyDefault",
                            argument => argument.DeprecationReason = "Deprecation Reason" )|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>(
                            "arg",
                            "description",
                            argument => { argument.DeprecationReason = "Deprecation Reason"; argument.DefaultValue = "MyDefault"; });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc4_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>(
                            "arg",
                            "description",
                            "MyDefault",
                            argument =>
                            {
                                argument.DeprecationReason = "Deprecation Reason";
                            })|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>(
                            "arg",
                            "description",
                            argument =>
                            {
                                argument.DeprecationReason = "Deprecation Reason";
                                argument.DefaultValue = "MyDefault";
                            });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc5_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>(
                            "arg",
                            "description",
                            "MyDefault",
                            argument =>
                                    {
                                    })|};
                }
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>(
                            "arg",
                            "description",
                            argument =>
                                    {
                                        argument.DefaultValue = "MyDefault";
                                    });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task Argument_WithDefaultValueArgument_WithConfigureFunc6_Fixed()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .{|#0:Argument<StringGraphType, string>(
                            "arg",
                            "description",
                            "MyDefault",
                            argument => { })|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text")
                        .Argument<StringGraphType>(
                            "arg",
                            "description",
                            argument => { argument.DefaultValue = "MyDefault"; });
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }
}
