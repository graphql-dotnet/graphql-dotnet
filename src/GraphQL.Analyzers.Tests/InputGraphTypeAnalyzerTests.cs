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
    public async Task NameExistsAsConstructorParameter_WithDifferentAccessibility(string ctorAccessibility, string ctorParams, bool isAllowed)
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
    public async Task NameExistsAsBaseConstructorArgument_GQL006()
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

        var expected = VerifyCS.Diagnostic().WithSpan(9, 32, 9, 43).WithArguments("FirstName", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
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
    public async Task FieldNameIsConstOrLiteral_NotExistsOnSourceType_GQL006(string fieldName)
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
        var expected = VerifyCS.Diagnostic().WithSpan(11, startColumn, 11, endColumn).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task NonGenericField_NameNotExistsOnSourceType_GQL006()
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

        var expected = VerifyCS.Diagnostic().WithSpan(9, 15, 9, 22).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task ThisField_NameNotExistsOnSourceType_GQL006()
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

        var expected = VerifyCS.Diagnostic().WithSpan(9, 37, 9, 44).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
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

        var expected = new[]
        {
            VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySourceType"),
            VerifyCS.Diagnostic().WithSpan(12, 32, 12, 41).WithArguments("Address", "MySourceType")
        };
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithTypeConstraint_GQL006()
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

        var expected = VerifyCS.Diagnostic().WithSpan(11, 32, 11, 39).WithArguments("Email", "MyBaseSource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithMultipleTypeConstraint_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource, IBaseSource
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                    Field<StringGraphType>("Address");
                }
            }

            public class MyBaseSource
            {
                public string Name { get; set; }
            }

            public interface IBaseSource
            {
                public string Address { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(11, 32, 11, 39).WithArguments("Email", "MyBaseSource or IBaseSource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithoutTypeConstraint_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public abstract class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic().WithSpan(9, 32, 9, 38).WithArguments("Name", "TSourceType"),
            VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "TSourceType"),
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_InputObjectGraphTypeWithClosedType_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public abstract class BaseInputObjectGraphType<TSomethingElse> : InputObjectGraphType<MySource>
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                }
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
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
                    Field<StringGraphType>("Address");
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource
            {
            }

            public class MySource : MyBaseSource
            {
                public string Name { get; set; }
            }

            public class MyBaseSource
            {
                public string Address { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWithGenericParameter_MoreDerivedTypeConstraint_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class AnotherBaseInputObjectGraphType<TSource> : BaseInputObjectGraphType<TSource>
                where TSource : MySource
            {
                public AnotherBaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                    Field<StringGraphType>("Address");
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource
            {
            }

            public class MySource : MyBaseSource
            {
                public string Name { get; set; }
            }

            public class MyBaseSource
            {
                public string Address { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(11, 32, 11, 39).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWith_BaseTypeDefinesSourceType_GQL006()
    {
        const string source = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputWithClosedTypeObjectGraphType
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>("Email");
                    Field<StringGraphType>("Address");
                }
            }

            public class BaseInputWithClosedTypeObjectGraphType : BaseInputObjectGraphType<MySource>
            {
            }

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource
            {
            }

            public class MySource : MyBaseSource
            {
                public string Name { get; set; }
            }

            public class MyBaseSource
            {
                public string Address { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
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
