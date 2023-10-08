using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.FieldNameAnalyzer,
    GraphQL.Analyzers.FieldNameCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class FieldNameAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldAndNameMethodsCalled_NotGraphQLBuilder_NoDiagnostics()
    {
        const string source = """
            namespace Sample.Server;

            public class MyType
            {
                public MyType()
                {
                    Field<string>().Name("Text");
                }

                private FieldBuilder<T> Field<T>()
                {
                    return new FieldBuilder<T>();
                }
            }

            public class FieldBuilder<T>
            {
                public FieldBuilder<T> Name(string name)
                {
                    return this;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldWithoutName_NameMethodCallInTheMiddle_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>().Name("Text").Resolve(context => "Test");
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
                    Field<StringGraphType>("Text").Resolve(context => "Test");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 34, 9, 46);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldWithoutName_NameMethodCallInTheEnd_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>().Resolve(context => "Test").Name("Text");
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
                    Field<StringGraphType>("Text").Resolve(context => "Test");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 61, 9, 73);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldWithName_NoNameMethodCalled_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").Resolve(context => "Test");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldAndNameMethodsHaveSameValues_NameMethodInvocationCanBeRemoved()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text").Name("Text").Resolve(context => "Test");
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
                    Field<StringGraphType>("Text").Resolve(context => "Test");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(9, 40, 9, 52);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldAndNameMethodsHaveSameValues_NamedArguments_NameMethodInvocationCanBeRemoved()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<string>(nullable: false, name: "Text").Name("Text").Resolve(context => "Test");
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
                    Field<string>(nullable: false, name: "Text").Resolve(context => "Test");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(9, 54, 9, 66);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldAndNameMethodsHaveSameExpressions_NameMethodInvocationCanBeRemoved()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(GetName()).Name(GetName()).Resolve(context => "Test");
                }

                private string GetName() => "Text";
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>(GetName()).Resolve(context => "Test");
                }

                private string GetName() => "Text";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(9, 43, 9, 58);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods(int codeActionIndex)
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1").Name("Text2").Resolve(context => "Test");
                }
            }
            """;

        const string fix0 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1").Resolve(context => "Test");
                }
            }
            """;

        const string fix1 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text2").Resolve(context => "Test");
                }
            }
            """;

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(9, 41, 9, 54);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods_CorrectlyFormatted(int codeActionIndex)
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1").Name("Text2")
                        .Resolve(context => "Test");
                }
            }
            """;

        const string fix0 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1")
                        .Resolve(context => "Test");
                }
            }
            """;

        const string fix1 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text2")
                        .Resolve(context => "Test");
                }
            }
            """;

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(9, 41, 9, 54);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods_CorrectlyFormatted2(int codeActionIndex)
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1").Resolve(context => "Test")
                        .Name("Text2");
                }
            }
            """;

        const string fix0 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text1").Resolve(context => "Test");
                }
            }
            """;

        const string fix1 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Text2").Resolve(context => "Test");
                }
            }
            """;

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(10, 14, 10, 27);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_FieldUsesNamedArguments_DifferentNamesDefinedByFieldAndNameMethods(int codeActionIndex)
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<string>(name: "Text1", nullable: true).Name("Text2").Resolve(context => "Test");
                }
            }
            """;

        const string fix0 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<string>(name: "Text1", nullable: true).Resolve(context => "Test");
                }
            }
            """;

        const string fix1 = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<string>(name: "Text2", nullable: true).Resolve(context => "Test");
                }
            }
            """;

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(9, 54, 9, 67);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldCalledOnVariableWithoutName_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.Field<StringGraphType>().Name("Text").Resolve(context => "Tests");
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
                    graphType.Field<StringGraphType>("Text").Resolve(context => "Tests");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 44, 9, 56);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenericFieldWithExpression_NameMethodCalled_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType<Person>
            {
                public MyGraphType()
                {
                    Field<string>(x => x.FullName).Name("Name");
                }
            }

            public class Person
            {
                public string FullName { get; set; }
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType<Person>
            {
                public MyGraphType()
                {
                    Field<string>("Name", x => x.FullName);
                }
            }

            public class Person
            {
                public string FullName { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 40, 9, 52);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task NonGenericFieldWithExpression_NameMethodCalled_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType<Person>
            {
                public MyGraphType()
                {
                    Field(x => x.FullName).Name("Name");
                }
            }

            public class Person
            {
                public string FullName { get; set; }
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType<Person>
            {
                public MyGraphType()
                {
                    Field("Name", x => x.FullName);
                }
            }

            public class Person
            {
                public string FullName { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 32, 9, 44);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldWithoutName_NameMethodCalled_NameOverloadUnknown_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>().Name();
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
                    Field<StringGraphType>();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 34, 9, 40);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            CompilerDiagnostics = CompilerDiagnostics.None,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync().ConfigureAwait(false);
    }
}
