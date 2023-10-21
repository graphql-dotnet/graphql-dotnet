using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.FieldNameAnalyzer,
    GraphQL.Analyzers.FieldNameCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class FieldNameAnalyzerTests
{
    private const string CONNECTION_BUILDER_CREATE = "ConnectionBuilder<string>.Create";

    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    public async Task FieldAndNameMethodsCalled_NotGraphQLBuilder_NoDiagnostics(string builderName)
    {
        string source = $$"""
            namespace Sample.Server;

            public class MyType
            {
                public MyType()
                {
                    {{builderName}}<string>().Name("Text");
                }

                private FieldBuilder<T> Field<T>()
                {
                    return new FieldBuilder<T>();
                }

                private ConnectionBuilder<T> Connection<T>()
                {
                    return new ConnectionBuilder<T>();
                }
            }

            public class FieldBuilder<T>
            {
                public FieldBuilder<T> Name(string name)
                {
                    return this;
                }
            }

            public class ConnectionBuilder<T>
            {
                public ConnectionBuilder<T> Name(string name)
                {
                    return this;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldWithoutName_NameMethodCallInTheMiddle_DefineTheNameInFieldMethod(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>().Name("Text").Description("description");
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text").Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>().".Length + 1;
        int endColumn = startColumn + "Name(\"Text\")".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldWithoutName_NameMethodCallInTheEnd_DefineTheNameInFieldMethod(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>().Description("description").Name("Text");
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text").Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>().Description(\"description\").".Length + 1;
        int endColumn = startColumn + "Name(\"Text\")".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldWithName_NoNameMethodCalled_NoDiagnostics(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text").Description("description");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldAndNameMethodsHaveSameValues_NameMethodInvocationCanBeRemoved(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text").Name("Text").Description("description");
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text").Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>(\"Text\").".Length + 1;
        int endColumn = startColumn + "Name(\"Text\")".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field, "name: \"Text\"")]
    [InlineData(Constants.MethodNames.Field, "nullable: false, name: \"Text\"")]
    [InlineData(Constants.MethodNames.Connection, "name: \"Text\"")]
    [InlineData(CONNECTION_BUILDER_CREATE, "name: \"Text\"")]
    public async Task FieldAndNameMethodsHaveSameValues_NamedArguments_NameMethodInvocationCanBeRemoved(string builderName, string builderArgs)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>({{builderArgs}}).Name("Text").Description("description");
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>({{builderArgs}}).Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>({builderArgs}).".Length + 1;
        int endColumn = startColumn + "Name(\"Text\")".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldAndNameMethodsHaveSameExpressions_NameMethodInvocationCanBeRemoved(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>(GetName()).Name(GetName()).Description("description");
                }

                private string GetName() => "Text";
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>(GetName()).Description("description");
                }

                private string GetName() => "Text";
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>(GetName()).".Length + 1;
        int endColumn = startColumn + "Name(GetName())".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.NameMethodInvocationCanBeRemoved).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field, 0)]
    [InlineData(Constants.MethodNames.Field, 1)]
    [InlineData(Constants.MethodNames.Connection, 0)]
    [InlineData(Constants.MethodNames.Connection, 1)]
    [InlineData(CONNECTION_BUILDER_CREATE, 0)]
    [InlineData(CONNECTION_BUILDER_CREATE, 1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods(string builderName, int codeActionIndex)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1").Name("Text2").Description("description");
                }
            }
            """;

        string fix0 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1").Description("description");
                }
            }
            """;

        string fix1 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text2").Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>(\"Text1\").".Length + 1;
        int endColumn = startColumn + "Name(\"Text2\")".Length;
        string methodName = GetMethodName(builderName);

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName, methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field, 0)]
    [InlineData(Constants.MethodNames.Field, 1)]
    [InlineData(Constants.MethodNames.Connection, 0)]
    [InlineData(Constants.MethodNames.Connection, 1)]
    [InlineData(CONNECTION_BUILDER_CREATE, 0)]
    [InlineData(CONNECTION_BUILDER_CREATE, 1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods_CorrectlyFormatted(string builderName, int codeActionIndex)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1").Name("Text2")
                        .Description("description");
                }
            }
            """;

        string fix0 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1")
                        .Description("description");
                }
            }
            """;

        string fix1 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text2")
                        .Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>(\"Text1\").".Length + 1;
        int endColumn = startColumn + "Name(\"Text2\")".Length;
        string methodName = GetMethodName(builderName);

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName, methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field, 0)]
    [InlineData(Constants.MethodNames.Field, 1)]
    [InlineData(Constants.MethodNames.Connection, 0)]
    [InlineData(Constants.MethodNames.Connection, 1)]
    [InlineData(CONNECTION_BUILDER_CREATE, 0)]
    [InlineData(CONNECTION_BUILDER_CREATE, 1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_DifferentNamesDefinedByFieldAndNameMethods_CorrectlyFormatted2(string builderName, int codeActionIndex)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1").Description("description")
                        .Name("Text2");
                }
            }
            """;

        string fix0 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text1").Description("description");
                }
            }
            """;

        string fix1 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>("Text2").Description("description");
                }
            }
            """;

        int startColumn = $"            .".Length + 1;
        int endColumn = startColumn + "Name(\"Text2\")".Length;
        string methodName = GetMethodName(builderName);

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(11, startColumn, 11, endColumn).WithArguments(methodName, methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field, 0)]
    [InlineData(Constants.MethodNames.Field, 1)]
    [InlineData(Constants.MethodNames.Connection, 0)]
    [InlineData(Constants.MethodNames.Connection, 1)]
    [InlineData(CONNECTION_BUILDER_CREATE, 0)]
    [InlineData(CONNECTION_BUILDER_CREATE, 1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_FieldUsesNamedArguments_DifferentNamesDefinedByFieldAndNameMethods(string builderName, int codeActionIndex)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>(name: "Text1").Name("Text2").Description("description");
                }
            }
            """;

        string fix0 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>(name: "Text1").Description("description");
                }
            }
            """;

        string fix1 = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>(name: "Text2").Description("description");
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>(name: \"Text1\").".Length + 1;
        int endColumn = startColumn + "Name(\"Text2\")".Length;
        string methodName = GetMethodName(builderName);

        string[] fixes = new[] { fix0, fix1 };

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName, methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FieldAndNameMethodsHaveDifferentNames_FieldUsesNamedArguments_DifferentNamesDefinedByFieldAndNameMethods2(int codeActionIndex)
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<string>(name: "Text1", nullable: true).Name("Text2").Description("description");
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
                    Field<string>(name: "Text1", nullable: true).Description("description");
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
                    Field<string>(name: "Text2", nullable: true).Description("description");
                }
            }
            """;

        string[] fixes = new[] { fix0, fix1 };
        string methodName = Constants.MethodNames.Field;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DifferentNamesDefinedByFieldAndNameMethods).WithSpan(9, 54, 9, 67).WithArguments(methodName, methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixes[codeActionIndex],
            CodeActionIndex = codeActionIndex,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    public async Task FieldCalledOnVariableWithoutName_DefineTheNameInFieldMethod(string builderName)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.{{builderName}}<StringGraphType>().Name("Text").Resolve(context => "Tests");
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.{{builderName}}<StringGraphType>("Text").Resolve(context => "Tests");
                }
            }
            """;

        int startColumn = $"        graphType.{builderName}<StringGraphType>().".Length + 1;
        int endColumn = startColumn + "Name(\"Text\")".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, startColumn, 9, endColumn).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
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

        const string methodName = Constants.MethodNames.Field;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 40, 9, 52).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
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

        const string methodName = Constants.MethodNames.Field;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(9, 32, 9, 44).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    [InlineData(Constants.MethodNames.Field)]
    [InlineData(Constants.MethodNames.Connection)]
    [InlineData(CONNECTION_BUILDER_CREATE)]
    public async Task FieldWithoutName_NameMethodCalled_NameOverloadUnknown_DefineTheNameInFieldMethod(string builderName)
    {
        string source = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>().Name();
                }
            }
            """;

        string fix = $$"""
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {{builderName}}<StringGraphType>();
                }
            }
            """;

        int startColumn = $"        {builderName}<StringGraphType>().".Length + 1;
        int endColumn = startColumn + "Name()".Length;
        string methodName = GetMethodName(builderName);

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(10, startColumn, 10, endColumn).WithArguments(methodName);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            CompilerDiagnostics = CompilerDiagnostics.None,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task NonGenericConnectionBuilderCreateMethod_NameMethodCalled_DefineTheNameInFieldMethod()
    {
        const string source = """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    ConnectionBuilder.Create<StringGraphType, string>().Name("Text").Description("description");
                }
            }
            """;

        const string fix = """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    ConnectionBuilder.Create<StringGraphType, string>("Text").Description("description");
                }
            }
            """;

        const string methodName = Constants.MethodNames.Create;

        var expected = VerifyCS.Diagnostic(FieldNameAnalyzer.DefineTheNameInFieldMethod).WithSpan(10, 61, 10, 73).WithArguments(methodName);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    private static string GetMethodName(string builder) =>
        builder != CONNECTION_BUILDER_CREATE ? builder : Constants.MethodNames.Create;
}
