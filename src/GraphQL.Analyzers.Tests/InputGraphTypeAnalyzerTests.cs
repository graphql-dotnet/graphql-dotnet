using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.InputGraphTypeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class InputGraphTypeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    // Good
    [InlineData("public string Name;", true)]
    [InlineData("public string Name { get; set; }", true)]
    [InlineData("public string Name { get; init; }", true)]
    // private
    [InlineData("private string Name;", false)]
    [InlineData("private string Name { get; set; }", false)]
    // static
    [InlineData("public static string Name;", false)]
    [InlineData("public static string Name { get; set; }", false)]
    // not settable
    [InlineData("public const string Name = \"ConstName\";", false)]
    [InlineData("public string Name { get; private set; }", false)]
    [InlineData("public string Name { get; }", false)]
    public async Task NameExistsAsFieldOrProperty_WithDifferentAccessibility(string sourceMember, bool isAllowed)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class MySourceType
            {
                {{sourceMember}}
            }
            """;

        var expected = isAllowed
            ? DiagnosticResult.EmptyDiagnosticResults
            : new[] { VerifyCS.Diagnostic().WithSpan(9, 32, 9, 38).WithArguments("Name", "MySourceType") };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Theory]
    // Good
    [InlineData("public", "string FirstName", true)]
    [InlineData("public", "string firstName", true)]
    [InlineData("public", "string firstName, int age", true)]
    // private ctor
    [InlineData("private", "string FirstName", false)]
    [InlineData("private", "string firstName", false)]
    [InlineData("private", "string firstName, int age", false)]
    public async Task NameDefinedAsConstructorParameter_WithDifferentAccessibility(string ctorAccessibility, string ctorParams, bool isAllowed)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("FirstName");
                }
            }

            public class MySourceType
            {
                {{ctorAccessibility}} MySourceType({{ctorParams}}) { }

                public string Name { get; set; }
            }
            """;

        var expected = isAllowed
            ? DiagnosticResult.EmptyDiagnosticResults
            : new[] { VerifyCS.Diagnostic().WithSpan(9, 32, 9, 43).WithArguments("FirstName", "MySourceType") };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task NameDefinedAsBaseConstructorArgument_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("FirstName");
                }
            }

            public class MySourceType : MyBaseSourceType
            {
                public MySourceType(string anotherName)
                    : base(anotherName) { }
            }

            public class MyBaseSourceType
            {
                public MyBaseSourceType(string firstName) { }

                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, VerifyCS.Diagnostic().WithSpan(9, 32, 9, 43).WithArguments("FirstName", "MySourceType")).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldNameExistsOnBaseSourceType_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class MySourceType : MyBaseSourceType
            {
                public string Email { get; set; }
            }

            public class MyBaseSourceType
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task FieldNameDoesNotExistOnSourceType_NotInputObjectGraphType_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType<MySourceType>
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("Email");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task NonGenericInputObjectGraphType_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task SourceTypeIsObject_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<object>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("\"Email\"")]
    [InlineData("EmailConst")]
    [InlineData("Consts.EmailConst")]
    public async Task FieldNameIsConstOrLiteral_DoesNotExistOnSourceType_GQL006(string fieldName)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                private const string EmailConst = "Email";

                public MyInputGraphType()
                {
                    Field<StringGraphType>({{fieldName}});
                    Field<StringGraphType>("Name");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }

            public class Consts
            {
                public const string EmailConst = "Email";
            }
            """;

        const int startColumn = 32;
        int endColumn = startColumn + fieldName.Length;
        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic().WithSpan(11, startColumn, 11, endColumn).WithArguments("Email", "MySourceType")).ConfigureAwait(false);
    }

    [Fact]
    public async Task NonGenericField_NameDoesNotExistOnSourceType_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field("Email", typeof(StringGraphType));
                    Field("Name", typeof(StringGraphType));
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic().WithSpan(9, 15, 9, 22).WithArguments("Email", "MySourceType")).ConfigureAwait(false);
    }

    [Fact]
    public async Task ThisField_NameDoesNotExistOnSourceType_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    this.Field<StringGraphType>("Email");
                    this.Field<StringGraphType>("Name");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic().WithSpan(9, 37, 9, 44).WithArguments("Email", "MySourceType")).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeprecatedFieldName_DoesNotExistOnSourceType_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Email").DeprecationReason("Deprecated field. Ignored!");
                    Field<StringGraphType>("Name");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task MultipleFieldNamesNotExistOnSourceType_MultipleGQL006Diagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                    Field<IntGraphType>("Age");
                    Field<StringGraphType>("Address");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySourceType"),
            VerifyCS.Diagnostic().WithSpan(12, 32, 12, 41).WithArguments("Address", "MySourceType")).ConfigureAwait(false);
    }

    [Fact]
    // TODO: add expected diagnostic for 'Email' field when input types inheritance support is added
    public async Task BaseInputTypeWithGenericParameter_NoDiagnostics()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                }
            }

            public class MyBaseSource
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWithGenericParameter_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
            {
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            source,
            VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource")).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"FirstName\", ")]
    public async Task FieldDefinedWithExpression_NoDiagnostics(string nameOverride)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field({{nameOverride}}source => source.Name);
                }
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }
}
