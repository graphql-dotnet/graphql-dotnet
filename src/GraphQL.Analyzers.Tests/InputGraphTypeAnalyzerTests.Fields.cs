using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.InputGraphTypeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public partial class InputGraphTypeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
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
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyInputGraphType : InputObjectGraphType<MySourceType>
              {
                  public MyInputGraphType()
                  {
                      Field<StringGraphType>({|#0:"Name"|});
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
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotSetSourceField)
                    .WithLocation(0).WithArguments("Name", symbolType, "Name", "MySourceType", reason)
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
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
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyInputGraphType : InputObjectGraphType<MySourceType>
              {
                  public MyInputGraphType()
                  {
                      Field<StringGraphType>({|#0:"Name"|});
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
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotSetSourceField)
                    .WithLocation(0).WithArguments("Name", "field", "name", "MySourceType", "'readonly'"),
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotSetSourceField)
                    .WithLocation(0).WithArguments("Name", "property", "Name", "MySourceType", "not 'public' and doesn't have a public setter")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
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
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyInputGraphType : InputObjectGraphType<{|#1:MySourceType|}>
              {
                  public MyInputGraphType()
                  {
                      Field<StringGraphType>({|#0:"FirstName"|});
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
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
                    .WithLocation(1).WithArguments("MySourceType"),
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                    .WithLocation(0).WithArguments("FirstName", "MySourceType")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DefaultConstructor_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<DefaultConstructor>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>({|#0:"FirstName"|});
                }
            }

            public class DefaultConstructor
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("FirstName", "DefaultConstructor");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ParameterlessConstructor_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<ParameterlessConstructor>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>({|#0:"FirstName"|});
                }
            }

            public class ParameterlessConstructor
            {
                // this constructor is chosen
                public ParameterlessConstructor() { }

                // this constructor is ignored
                public ParameterlessConstructor(string firstName) { }

                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("FirstName", "ParameterlessConstructor");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task SingleConstructor_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<SingleConstructor>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("FirstName");
                }
            }

            public class SingleConstructor
            {
                // single constructor is chosen
                public SingleConstructor(string firstName) { }

                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConstructorWithGraphQLConstructorAttribute_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<ConstructorWithGraphQLConstructorAttribute>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("FirstName");
                }
            }

            public class ConstructorWithGraphQLConstructorAttribute
            {
                // this constructor is ignored
                public ConstructorWithGraphQLConstructorAttribute() { }

                // this constructor is chosen
                [GraphQLConstructor]
                public ConstructorWithGraphQLConstructorAttribute(string firstName) { }

                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MultipleConstructorWithGraphQLConstructorAttribute_GQL006_And_GQL010()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<{|#0:ConstructorWithGraphQLConstructorAttribute|}>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>({|#1:"FirstName"|});
                }
            }

            // all constructors are ignored
            public class ConstructorWithGraphQLConstructorAttribute
            {
                public ConstructorWithGraphQLConstructorAttribute(string firstName) { }

                [GraphQLConstructor]
                public ConstructorWithGraphQLConstructorAttribute(string firstName, int x) { }

                [GraphQLConstructor]
                public ConstructorWithGraphQLConstructorAttribute(string firstName, int x, int y) { }

                public string Name { get; set; }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                .WithLocation(1).WithArguments("FirstName", "ConstructorWithGraphQLConstructorAttribute"),
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
                .WithLocation(0).WithArguments("ConstructorWithGraphQLConstructorAttribute")
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NameExistsAsBaseConstructorArgument_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>({|#0:"FirstName"|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("FirstName", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FieldNameExistsOnBaseSourceType_NoDiagnostics()
    {
        const string source =
            """
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

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task FieldNameDoesNotExistOnSourceType_NotInputObjectGraphType_NoDiagnostics()
    {
        const string source =
            """
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

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NonGenericInputObjectGraphType_NoDiagnostics()
    {
        const string source =
            """
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

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SourceTypeIsObject_NoDiagnostics()
    {
        const string source =
            """
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

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("\"Email\"")]
    [InlineData("EmailConst")]
    [InlineData("Consts.EmailConst")]
    public async Task FieldNameIsConstOrLiteral_NotExistsOnSourceType_GQL006(string fieldName)
    {
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyInputGraphType : InputObjectGraphType<MySourceType>
              {
                  private const string EmailConst = "Email";

                  public MyInputGraphType()
                  {
                      Field<StringGraphType>({|#0:{{fieldName}}|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task NonGenericField_NameNotExistsOnSourceType_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field({|#0:"Email"|}, typeof(StringGraphType));
                    Field("Name", typeof(StringGraphType));
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ThisField_NameNotExistsOnSourceType_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    this.Field<StringGraphType>({|#0:"Email"|});
                    this.Field<StringGraphType>("Name");
                }
            }

            public class MySourceType
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySourceType");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task MultipleFieldNamesNotExistOnSourceType_MultipleGQL006Diagnostics()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyInputGraphType : InputObjectGraphType<MySourceType>
            {
                public MyInputGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
                    Field<IntGraphType>("Age");
                    Field<StringGraphType>({|#1:"Address"|});
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
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                .WithLocation(0).WithArguments("Email", "MySourceType"),
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                .WithLocation(1).WithArguments("Address", "MySourceType")
        };
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithTypeConstraint_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
                }
            }

            public class MyBaseSource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MyBaseSource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithMultipleTypeConstraint_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
                where TSourceType : MyBaseSource, IBaseSource
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MyBaseSource or IBaseSource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_WithoutTypeConstraint_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public abstract class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>({|#0:"Name"|});
                    Field<StringGraphType>({|#1:"Email"|});
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                .WithLocation(0).WithArguments("Name", "TSourceType"),
            VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
                .WithLocation(1).WithArguments("Email", "TSourceType"),
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BaseInputTypeWithGenericParameter_InputObjectGraphTypeWithClosedType_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public abstract class BaseInputObjectGraphType<TSomethingElse> : InputObjectGraphType<MySource>
            {
                public BaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
                }
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWithGenericParameter_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField).WithLocation(0).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWithGenericParameter_MoreDerivedTypeConstraint_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class AnotherBaseInputObjectGraphType<TSource> : BaseInputObjectGraphType<TSource>
                where TSource : MySource
            {
                public AnotherBaseInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DerivedFromBaseInputTypeWith_BaseTypeDefinesSourceType_GQL006()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputWithClosedTypeObjectGraphType
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Email"|});
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

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Email", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("private", false)]
    public async Task FieldDefinedWithExpression_NoNameOverride_GQL007(string setterAccessibility, bool isAllowed)
    {
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class CustomInputObjectGraphType : InputObjectGraphType<MySourceType>
              {
                  public CustomInputObjectGraphType()
                  {
                      Field(source => {|#0:source.Name|});
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
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotSetSourceField)
                    .WithLocation(0).WithArguments("Name", "property", "Name", "MySourceType", "doesn't have a public setter")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("private", false)]
    public async Task FieldDefinedWithExpression_WithNameOverride_GQL007(string setterAccessibility, bool isAllowed)
    {
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class CustomInputObjectGraphType : InputObjectGraphType<MySourceType>
              {
                  public CustomInputObjectGraphType()
                  {
                      Field({|#0:"FirstName"|}, source => source.Name);
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
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotSetSourceField)
                    .WithLocation(0).WithArguments("FirstName", "property", "Name", "MySourceType", "doesn't have a public setter")
            };

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task BaseInputObjectGraphType_OverridesParseDictionary_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
            {
                public override object ParseDictionary(IDictionary<string, object> value) =>
                    base.ParseDictionary(value);
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task BaseInputObjectGraphType_OverridesParseDictionary_NotDefinedByTheInputObjectGraphType_GQL007()
    {
        const string source =
            """
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Address"|});
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : BaseBaseInputObjectGraphType<TSourceType>
            {
                public override object ParseDictionary(IDictionary<string, object> value) =>
                    base.ParseDictionary(value);
            }

            public class BaseBaseInputObjectGraphType<TSourceType> : InputObjectGraphType<TSourceType>
            {
                // hide the original method
                public new virtual object ParseDictionary(IDictionary<string, object> value) =>
                    base.ParseDictionary(value);
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Address", "MySource");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("Sample.Server.BaseInputObjectGraphType", false)]
    [InlineData("Sample.Server.BaseInputObjectGraphType, Sample.Server.BaseInputObjectGraphType2", true)]
    public async Task BaseInputObjectGraphType_OverridesParseDictionary_ForceTypesAnalysis_GQL007(string forceTypes, bool report)
    {
        const string source =
            """
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                    Field<StringGraphType>({|#0:"Address"|});
                }
            }

            public class BaseInputObjectGraphType<TSourceType> : BaseInputObjectGraphType2<TSourceType>
            {
                public override object ParseDictionary(IDictionary<string, object> value) =>
                    base.ParseDictionary(value);
            }

            public class BaseInputObjectGraphType2<TSourceType> : InputObjectGraphType<TSourceType>
            {
                public override object ParseDictionary(IDictionary<string, object> value) =>
                    base.ParseDictionary(value);
            }

            public class MySource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotMatchInputFieldToTheSourceField)
            .WithLocation(0).WithArguments("Address", "MySource");

        var test = new VerifyCS.Test
        {
            TestCode = source,
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $$"""
                         root = true

                         [*]
                         {{InputGraphTypeAnalyzer.ForceTypesAnalysisOption}} = {{forceTypes}}
                         ")
                         """)
                }
            }
        };

        if (report)
        {
            test.ExpectedDiagnostics.Add(expected);
        }

        await test.RunAsync();
    }
}
