using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.OneOfAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class OneOfAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("this.")]
    public async Task CodeFirst_OneOfIsTrue_AllFieldsAreNullable_NoDiagnostics(string? prefix)
    {
        string source =
            $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class OneOfInput : InputObjectGraphType<MyInput>
            {
                public OneOfInput()
                {
                    {{prefix}}IsOneOf = true;
                    {{prefix}}Field<StringGraphType>("name");
                    {{prefix}}Field(x => x.Name, nullable: true);
                    {{prefix}}Field(x => x.Name, type: typeof(StringGraphType));
                }
            }

            public class MyInput
            {
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("this.", true)]
    [InlineData(null, false)]
    [InlineData("this.", false)]
    public async Task CodeFirst_AllFieldsAreNotNullable_DiagnosticsReportedWhenIsOneOfTrue(string? prefix, bool isOneOf)
    {
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class OneOfInput : InputObjectGraphType<MyInput>
              {
                  public OneOfInput()
                  {
                      {{prefix}}IsOneOf = {{isOneOf.ToString().ToLower()}};
                      {{prefix}}{|#0:Field<NonNullGraphType<StringGraphType>>|}("name");
                      {{prefix}}{|#1:Field|}(x => x.Name);
                      {{prefix}}{|#2:Field|}(x => x.Name, nullable: false);
                      {{prefix}}{|#3:Field|}(x => x.Name, type: typeof(NonNullGraphType<StringGraphType>));
                  }
              }

              public class MyInput
              {
                  public string Name { get; set; }
              }
              """;

        var expected = isOneOf
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(2),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(3)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CodeFirst_FieldHasDefaultValue_ReportDiagnosticsWhenIsOneOfTrue(bool isOneOf)
    {
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class OneOfInput : InputObjectGraphType
              {
                  public OneOfInput()
                  {
                      IsOneOf = {{isOneOf.ToString().ToLower()}};
                      Field<StringGraphType>("name").{|#0:DefaultValue|}("Joe");
                  }
              }
              """;

        var expected = isOneOf
            ? [VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(0)]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[OneOf]")]
    [InlineData("[OneOfAttribute]")]
    public async Task TypeFirst_NonNullableDiagnosticsReportedWhenOneOfAttribute(string? attribute)
    {
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              {{attribute}}
              public class MyInput
              {
                  public int? NullableProp { get; set; }
                  public int? NullableField;

                  public {|#0:int|} NonNullableProp { get; set; }
                  public {|#1:int|} NonNullableField;

              #nullable disable
                  public string NullableRefProp1 { get; set; }
                  public string NullableRefField1;

              #nullable enable
                  public string? NullableRefProp2 { get; set; }
                  public string? NullableRefField2;

                  public {|#2:string|} NonNullableRefProp { get; set; } = {|#3:null!|};
                  public {|#4:string|} NonNullableRefField = {|#5:null!|};
              }
              """;

        var expected = attribute != null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(2),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(3),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(4),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(5)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[OneOf]")]
    [InlineData("[OneOfAttribute]")]
    public async Task TypeFirst_DefaultValueDiagnosticsReportedWhenOneOfAttribute(string? attribute)
    {
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              {{attribute}}
              public class MyInput
              {
                  public int? NullableProp { get; set; } = {|#0:1|};
                  public int? NullableField = {|#1:1|};

              #nullable disable
                  public string NullableRefProp1 { get; set; } = {|#2:"xxx"|};
                  public string NullableRefField1 = {|#3:"xxx"|};

              #nullable enable
                  public string? NullableRefProp2 { get; set; } = {|#4:"yyy"|};
                  public string? NullableRefField2 = {|#5:"yyy"|};
              }
              """;

        var expected = attribute != null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(2),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(3),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(4),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(5)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[Ignore]")]
    public async Task TypeFirst_DiagnosticsReportedWhenNotIgnored(string? attribute)
    {
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              [OneOf]
              public class MyInput
              {
                  {{attribute}}
                  public {|#0:int|} NonNullableProp { get; set; } = {|#1:42|};

                  {{attribute}}
                  public {|#2:int|} NonNullableField = {|#3:42|};
              }
              """;

        var expected = attribute == null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(2),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(3)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
