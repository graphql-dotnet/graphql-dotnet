using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.FieldBuilderAnalyzer,
    GraphQL.Analyzers.FieldBuilderCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class FieldBuilderAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = @"";

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task AllArgumentsProvided_NoNamedArguments_FixProvided()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(
                        "name",
                        "description",
                        new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }),
                        context => "text",
                        "deprecated reason");
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
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }))
                        .Resolve(context => "text")
                        .DeprecationReason("deprecated reason");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 14, 33);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task NamedArgumentsNotInOrder_FixPreservesSameOrder()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(
                        name: "name",
                        deprecationReason: "deprecated reason",
                        description: "description",
                        resolve: context => "text",
                        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }));
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
                    Field<StringGraphType>("name")
                        .DeprecationReason("deprecated reason")
                        .Description("description")
                        .Resolve(context => "text")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }));
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 14, 100);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task NameArgumentIsNotFirst_FixCorrectly()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(
                        resolve: context => "text",
                        description: "description",
                        name: "name");
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
                    Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .Description("description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 12, 26);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldCalledOnVariable_FixCorrectly()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.Field<StringGraphType>(
                        name: "name",
                        resolve: context => "text",
                        description: "description");
                }
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .Description("description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 12, 40);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldAsync_FixAsFieldAndResolveAsync()
    {
        const string source = """
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    FieldAsync<StringGraphType>(
                        "name",
                        "description",
                        resolve: context => Task.FromResult<object>("text"));
                }
            }
            """;

        const string fix = """
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Description("description")
                        .ResolveAsync(context => Task.FromResult<object>("text"));
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(10, 9, 13, 65);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task ArgumentsListMultilineFormatted_FormattingPreserved()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(
                        "name",
                        arguments: new QueryArguments(new QueryArgument<StringGraphType>
                        {
                            Name = "argName"
                        }),
                        resolve: context => "text");
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
                    Field<StringGraphType>("name")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType>
                        {
                            Name = "argName"
                        }))
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 15, 40);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task NonGenericFieldMethod_FixProvided()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field(
                        typeof(string),
                        "name",
                        resolve: context => "text");
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
                    Field("name", typeof(string))
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 12, 40);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldSubscribe_SubscribeArgument_ConvertToFieldAndResolveStream()
    {
        const string source = """
            using System;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    FieldSubscribe<StringGraphType>(
                        "name",
                        resolve: context => "text",
                        subscribe: context => new Observable());
                }

                private class Observable : IObservable<string>
                {
                    public IDisposable Subscribe(IObserver<string> observer) =>
                        throw new NotImplementedException();
                }
            }
            """;

        const string fix = """
            using System;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .ResolveStream(context => new Observable());
                }

                private class Observable : IObservable<string>
                {
                    public IDisposable Subscribe(IObserver<string> observer) =>
                        throw new NotImplementedException();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(10, 9, 13, 52);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldDelegate_ConvertToFieldAndResolveDelegate()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    FieldDelegate<StringGraphType>(
                        "name",
                        resolve: TestMethod);
                }

                public string TestMethod() => "test";
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(TestMethod);
                }

                public string TestMethod() => "test";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 11, 33);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task ArgumentsFormattingPreservedAsync()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name", "description", null,
                        resolve: context => GetName(
                            "s1",
                              "s2",
                                "s3"
                        ));
                }

                public string GetName(string s1, string s2, string s3) => "text";
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name").Description("description")
                        .Resolve(context => GetName(
                            "s1",
                              "s2",
                                "s3"
                        ));
                }

                public string GetName(string s1, string s2, string s3) => "text";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 14, 15);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task ArgumentsFormattingPreserved_PreserveNewLines()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {

                    Field<StringGraphType>("name1", "description1", null,
                        context => "text1");

                    var str = "string";

                    Field<StringGraphType>("name2", "description2", null,
                        context => "text2");
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

                    Field<StringGraphType>("name1").Description("description1")
                        .Resolve(context => "text1");

                    var str = "string";

                    Field<StringGraphType>("name2").Description("description2")
                        .Resolve(context => "text2");
                }
            }
            """;

        var expected1 = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(10, 9, 11, 32);
        var expected2 = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(15, 9, 16, 32);
        await VerifyCS.VerifyCodeFixAsync(source, new[] { expected1, expected2 }, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task ReformatOptionIsTrue_SourceReformatted()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name", "description", null,
                        context => "text");
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
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 10, 31);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            ExpectedDiagnostics = { expected },
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $"""
                        root = true

                        [*]
                        {FieldBuilderCodeFixProvider.ReformatOption} = true
                        ")
                        """)
                }
            }
        };
        await test.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task SkipNullsOptionIsFalse_NullArgumentsPreserved()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(
                        "name",
                        "description",
                        null,
                        context => "text");
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
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Arguments(null)
                        .Resolve(context => "text");
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            ExpectedDiagnostics = { VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithSpan(9, 9, 13, 31) },
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $"""
                        root = true

                        [*]
                        {FieldBuilderCodeFixProvider.SkipNullsOption} = false
                        ")
                        """)
                }
            }
        };
        await test.RunAsync().ConfigureAwait(false);
    }
}
