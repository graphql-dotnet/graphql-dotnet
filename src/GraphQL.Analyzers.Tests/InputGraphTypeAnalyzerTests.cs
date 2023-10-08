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
    [InlineData("private string Name;", false, "field", "not 'public'")]
    [InlineData("private string Name { get; set; }", false, "property", "not 'public' and doesn't have a public setter")]
    // static
    [InlineData("public static string Name;", false, "field", "'static'")]
    [InlineData("public static string Name { get; set; }", false, "property", "'static'")]
    // not settable
    [InlineData("public const string Name = \"ConstName\";", false, "field", "'const'")]
    [InlineData("public string Name { get; private set; }", false, "property", "doesn't have a public setter")]
    [InlineData("public string Name { get; }", false, "property", "doesn't have a public setter")]
    public async Task NameExistsAsFieldOrProperty_WithDifferentAccessibility_GQL007(
        string sourceMember,
        bool isAllowed,
        string symbolType = "",
        string reason = "")
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
            : new[]
            {
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD)
                    .WithSpan(9, 32, 9, 38).WithArguments("Name", symbolType, "Name", "MySourceType", reason)
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Theory]
    // Good
    [InlineData("public string Name { get; set; }", "public string name;", true)]
    [InlineData("public string name;", "public string Name { get; set; }", true)]
    // One good, one bad. Different order
    [InlineData("public string Name { get; set; }", "public readonly string name;", true)]
    [InlineData("public readonly string name;", "public string Name { get; set; }", true)]
    // Bad
    [InlineData("public readonly string name;", "private string Name { get; set; }", false)]
    public async Task SameNameExistsTwice_DifferentCasing_GQL007(
        string symbol1,
        string symbol2,
        bool isAllowed)
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
                {{symbol1}}
                {{symbol2}}
            }
            """;

        var expected = isAllowed
            ? DiagnosticResult.EmptyDiagnosticResults
            : new[]
            {
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD)
                    .WithSpan(9, 32, 9, 38).WithArguments("Name", "field", "name", "MySourceType", "'readonly'"),
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD)
                    .WithSpan(9, 32, 9, 38).WithArguments("Name", "property", "Name", "MySourceType", "not 'public' and doesn't have a public setter")
            };

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
    public async Task NameExistsAsConstructorParameter_WithDifferentAccessibility_GQL006(string ctorAccessibility, string ctorParams, bool isAllowed)
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
            : new[]
            {
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
                    .WithSpan(9, 32, 9, 43).WithArguments("FirstName", "MySourceType")
            };

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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(9, 32, 9, 43).WithArguments("FirstName", "MySourceType");
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
        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(11, startColumn, 11, endColumn).WithArguments("Email", "MySourceType");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(9, 15, 9, 22).WithArguments("Email", "MySourceType");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(9, 37, 9, 44).WithArguments("Email", "MySourceType");
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
            VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
                .WithSpan(10, 32, 10, 39).WithArguments("Email", "MySourceType"),
            VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
                .WithSpan(12, 32, 12, 41).WithArguments("Address", "MySourceType")
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(11, 32, 11, 39).WithArguments("Email", "MyBaseSource");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(11, 32, 11, 39).WithArguments("Email", "MyBaseSource or IBaseSource");
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
            VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
                .WithSpan(9, 32, 9, 38).WithArguments("Name", "TSourceType"),
            VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
                .WithSpan(10, 32, 10, 39).WithArguments("Email", "TSourceType"),
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD).WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(11, 32, 11, 39).WithArguments("Email", "MySource");
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

        var expected = VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_MATCH_INPUT_FIELD_TO_THE_SOURCE_FIELD)
            .WithSpan(10, 32, 10, 39).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("private", false)]
    public async Task FieldDefinedWithExpression_NoNameOverride_GQL007(string setterAccessibility, bool isAllowed)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : InputObjectGraphType<MySourceType>
            {
                public CustomInputObjectGraphType()
                {
                    Field(source => source.Name);
                }
            }

            public class MySourceType
            {
                public string Name { get; {{setterAccessibility}} set; }
            }
            """;

        var expected = isAllowed
            ? DiagnosticResult.EmptyDiagnosticResults
            : new[]
            {
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD)
                    .WithSpan(9, 25, 9, 36).WithArguments("Name", "property", "Name", "MySourceType", "doesn't have a public setter")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("private", false)]
    public async Task FieldDefinedWithExpression_WithNameOverride_GQL007(string setterAccessibility, bool isAllowed)
    {
        string source = $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : InputObjectGraphType<MySourceType>
            {
                public CustomInputObjectGraphType()
                {
                    Field("FirstName", source => source.Name);
                }
            }

            public class MySourceType
            {
                public string Name { get; {{setterAccessibility}} set; }
            }
            """;

        var expected = isAllowed
            ? DiagnosticResult.EmptyDiagnosticResults
            : new[]
            {
                VerifyCS.Diagnostic(DiagnosticIds.CAN_NOT_SET_SOURCE_FIELD)
                    .WithSpan(9, 15, 9, 26).WithArguments("FirstName", "property", "Name", "MySourceType", "doesn't have a public setter")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
    }
}
