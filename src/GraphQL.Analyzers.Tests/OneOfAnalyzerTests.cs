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
                  // Good
                  public int? NullableValue { get; set; }

                  // Bad
                  public {|#0:int|} NonNullableValue { get; set; }

              #nullable disable
                  // Good
                  public string NullableRefValue1 { get; set; }

              #nullable enable
                  // Good
                  public string? NullableRefValue2 { get; set; }

                  // Bad
                  public {|#1:string|} NonNullableRefValue { get; set; } = {|#2:null!|};
              }
              """;

        var expected = attribute != null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(2)
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
                  public int? NullableValue { get; set; } = {|#0:1|};

              #nullable disable
                  public string NullableRefValue1 { get; set; } = {|#1:"xxx"|};

              #nullable enable
                  public string? NullableRefValue2 { get; set; } = {|#2:"yyy"|};
              }
              """;

        var expected = attribute != null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(1),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(2)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[Ignore]")]
    public async Task TypeFirst_DiagnosticsReported_WhenNotIgnored(string? attribute)
    {
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              [OneOf]
              public class MyInput
              {
                  {{attribute}}
                  public {|#0:int|} NonNullableValue { get; set; } = {|#1:42|};
              }
              """;

        var expected = attribute == null
            ?
            [
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustBeNullable).WithLocation(0),
                VerifyCS.Diagnostic(OneOfAnalyzer.OneOfFieldsMustNotHaveDefaultValue).WithLocation(1)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
