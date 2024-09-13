using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.ValidateArgumentsAttributeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class ValidateArgumentsAttributeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, "[ValidateArguments(\"PrivateValidator\")]")]
    [InlineData(2, "[ValidateArguments(nameof(PrivateValidator))]")]
    [InlineData(3, "[ValidateArguments(typeof(TestClass), nameof(PrivateValidator))]")]
    [InlineData(4, "[ValidateArguments(ValidatorName1)]")]
    [InlineData(5, "[ValidateArguments(ValidatorName2)]")]
    [InlineData(6, "[ValidateArguments(Constants.ValidatorName)]")]
    [InlineData(7, "[ValidateArguments(NestingType.Constants.ValidatorName)]")]
    [InlineData(8, "[ValidateArguments(typeof(ValidatorClass))]")]
    [InlineData(9, "[ValidateArguments(typeof(ValidatorClass), nameof(ValidatorClass.ValidateValues))]")]
    public async Task ValidValidatorMethods_NoDiagnostics(int idx, string attribute)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;
              using GraphQL.Validation;
              using System.Threading.Tasks;

              namespace Sample.Server;

              public class TestClass
              {
                  private const string ValidatorName1 = nameof(PrivateValidator);
                  private const string ValidatorName2 = "PrivateValidator";

                  {{attribute}}
                  public static string Hello1(string value) => value;

                  private static ValueTask PrivateValidator(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
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
                  public static ValueTask ValidateArguments(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
                  public static ValueTask ValidateValues(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
                  public static ValueTask ValidateValues(object context) => ValueTask.CompletedTask; // invalid overload
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
    public async Task NoType_InvalidValidatorMethodSignature_GQL020(int idx, string methodName)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;
              using GraphQL.Validation;
              using System.Threading.Tasks;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[ValidateArguments(nameof({{methodName}}))]|}
                  public static string Hello1(string value) => value;

                  private ValueTask Validate1(FieldArgumentsValidationContext context) => ValueTask.CompletedTask; // not static
                  private static object Validate2(FieldArgumentsValidationContext context) => ValueTask.CompletedTask; // wrong return type
                  private static ValueTask Validate3(object context) => ValueTask.CompletedTask; // wrong parameter type
                  private static ValueTask Validate4(FieldArgumentsValidationContext context1, FieldArgumentsValidationContext context2) => ValueTask.CompletedTask; // too many parameters
                  private static ValueTask Validate5() => ValueTask.CompletedTask; // no parameters
              }
              """;

        var expected = VerifyCS.Diagnostic(ValidateArgumentsAttributeAnalyzer.ValidateArgumentsMethodMustBeValid)
            .WithLocation(0).WithArguments(methodName, "");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "internal")]
    [InlineData(2, "protected")]
    [InlineData(3, "private")]
    [InlineData(4, "")]
    public async Task OtherType_NonPublicMethod_GQL020(int idx, string accessor)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;
              using GraphQL.Validation;
              using System.Threading.Tasks;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[ValidateArguments(typeof(ValidatorClass))]|}
                  public static string Hello1(string value) => value;
              }

              public class ValidatorClass
              {
                  {{accessor}} static ValueTask ValidateArguments(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
              }
              """;

        var expected = VerifyCS.Diagnostic(ValidateArgumentsAttributeAnalyzer.ValidateArgumentsMethodMustBeValid)
            .WithLocation(0).WithArguments("ValidateArguments", "public ");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "TestClass", "[ValidateArguments(\"ValidateArguments\")]")]
    [InlineData(2, "TestClass", "[ValidateArguments(typeof(TestClass))]")]
    [InlineData(3, "TestClass", "[ValidateArguments(typeof(TestClass), \"ValidateArguments\")]")]
    [InlineData(4, "ValidatorClass", "[ValidateArguments(typeof(ValidatorClass))]")]
    [InlineData(5, "ValidatorClass", "[ValidateArguments(typeof(ValidatorClass), \"ValidateArguments\")]")]
    public async Task MethodNotFound_GQL017(int idx, string validatorType, string attribute)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;
              using GraphQL.Validation;
              using System.Threading.Tasks;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:{{attribute}}|}
                  public static string Hello1(string value) => value;

                  private static ValueTask ValidateValue(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
              }

              public class ValidatorClass
              {
                  public static ValueTask ValidateValue(FieldArgumentsValidationContext context) => ValueTask.CompletedTask;
              }
              """;

        var expected = VerifyCS.Diagnostic(ParserValidatorAttributeAnalyzer.CouldNotFindMethod)
            .WithLocation(0).WithArguments("ValidateArguments", validatorType);
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
