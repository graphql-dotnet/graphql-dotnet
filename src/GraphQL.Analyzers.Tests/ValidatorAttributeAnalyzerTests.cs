using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.ValidatorAttributeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class ValidatorAttributeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, "[Validator(\"PrivateValidator\")]")]
    [InlineData(2, "[Validator(nameof(PrivateValidator))]")]
    [InlineData(3, "[Validator(typeof(TestClass), nameof(PrivateValidator))]")]
    [InlineData(4, "[Validator(ValidatorName1)]")]
    [InlineData(5, "[Validator(ValidatorName2)]")]
    [InlineData(6, "[Validator(Constants.ValidatorName)]")]
    [InlineData(7, "[Validator(NestingType.Constants.ValidatorName)]")]
    [InlineData(8, "[Validator(typeof(ValidatorClass))]")]
    [InlineData(9, "[Validator(typeof(ValidatorClass), nameof(ValidatorClass.ValidateValue))]")]
    public async Task ValidValidatorMethods_NoDiagnostics(int idx, string attribute)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  private const string ValidatorName1 = nameof(PrivateValidator);
                  private const string ValidatorName2 = "PrivateValidator";

                  {{attribute}}
                  public string Hello { get; set; }

                  private static void PrivateValidator(object value) { }
              }

              public class Constants
              {
                  public const string ValidatorName = "PrivateValidator";
              }

              public class NestingType
              {
                  public class Constants
                  {
                      public const string ValidatorName = "PrivateValidator";
                  }
              }

              public class ValidatorClass
              {
                  public static void Validate(object value) { }
                  public static void ValidateValue(object value) { }
                  public static void ValidateValue(string value) { } // invalid overload
              }
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, "Validate1")]
    [InlineData(2, "Validate2")]
    [InlineData(3, "Validate3")]
    [InlineData(4, "Validate4")]
    [InlineData(5, "Validate5")]
    public async Task NoType_InvalidValidatorMethodSignature_GQL019(int idx, string methodName)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[Validator(nameof({{methodName}}))]|}
                  public string Hello { get; set; }

                  private void Validate1(object value) { } // not static
                  private static string Validate2(object value) => "value"; // wrong return type
                  private static void Validate3(string value) { } // wrong parameter type
                  private static void Validate4(object value1, object value2) { } // too many parameters
                  private static void Validate5() { } // no parameters
              }
              """;

        var expected = VerifyCS.Diagnostic(ValidatorAttributeAnalyzer.ValidatorMethodMustBeValid)
            .WithLocation(0).WithArguments(methodName, "");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "internal")]
    [InlineData(2, "protected")]
    [InlineData(3, "private")]
    [InlineData(4, "")]
    public async Task OtherType_NonPublicMethod_GQL019(int idx, string accessor)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[Validator(typeof(ValidatorClass))]|}
                  public string Hello { get; set; }
              }

              public class ValidatorClass
              {
                  {{accessor}} static void Validate(object value) { }
              }
              """;

        var expected = VerifyCS.Diagnostic(ValidatorAttributeAnalyzer.ValidatorMethodMustBeValid)
            .WithLocation(0).WithArguments("Validate", "public ");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "TestClass", "[Validator(\"Validate\")]")]
    [InlineData(2, "TestClass", "[Validator(typeof(TestClass))]")]
    [InlineData(3, "TestClass", "[Validator(typeof(TestClass), \"Validate\")]")]
    [InlineData(4, "ValidatorClass", "[Validator(typeof(ValidatorClass))]")]
    [InlineData(5, "ValidatorClass", "[Validator(typeof(ValidatorClass), \"Validate\")]")]
    public async Task MethodNotFound_GQL017(int idx, string validatorType, string attribute)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:{{attribute}}|}
                  public string Hello { get; set; }

                  private static object ValidateValue(object value) => value;
              }

              public class ValidatorClass
              {
                  public static object ValidateValue(object value) => value;
              }
              """;

        var expected = VerifyCS.Diagnostic(ParserValidatorAttributeAnalyzer.CouldNotFindMethod)
            .WithLocation(0).WithArguments("Validate", validatorType);
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
