using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.ParserAttributeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class ParserAttributeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, "[Parser(\"PrivateParser\")]")]
    [InlineData(2, "[Parser(nameof(PrivateParser))]")]
    [InlineData(3, "[Parser(typeof(TestClass), nameof(PrivateParser))]")]
    [InlineData(4, "[Parser(ParserName1)]")]
    [InlineData(5, "[Parser(ParserName2)]")]
    [InlineData(6, "[Parser(Constants.ParserName)]")]
    [InlineData(7, "[Parser(NestingType.Constants.ParserName)]")]
    [InlineData(8, "[Parser(typeof(ParserClass))]")]
    [InlineData(9, "[Parser(typeof(ParserClass), nameof(ParserClass.ParseValue))]")]
    [InlineData(10, "[Parser(typeof(ParserClass), nameof(ParserClass.ParseWithConverter))]")]
    [InlineData(11, "[Parser(nameof(PrivateParserWithConverter))]")]
    public async Task ValidParserMethods_NoDiagnostics(int idx, string attribute)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  private const string ParserName1 = nameof(PrivateParser);
                  private const string ParserName2 = "PrivateParser";

                  {{attribute}}
                  public string Hello { get; set; }

                  private static object PrivateParser(object value) => value;
                  private static object PrivateParserWithConverter(object value, IValueConverter converter) => value;
              }

              public class Constants
              {
                  public const string ParserName = "PrivateParser";
              }

              public class NestingType
              {
                  public class Constants
                  {
                      public const string ParserName = "PrivateParser";
                  }
              }

              public class ParserClass
              {
                  public static object Parse(object value) => value;
                  public static object ParseValue(object value) => value;
                  public static object ParseValue(string value) => value; // invalid overload
                  public static object ParseWithConverter(object value, IValueConverter converter) => value;
              }
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData(1, "Parse1")]
    [InlineData(2, "Parse2")]
    [InlineData(3, "Parse3")]
    [InlineData(4, "Parse4")]
    [InlineData(5, "Parse5")]
    [InlineData(6, "Parse6")]
    [InlineData(7, "Parse7")]
    [InlineData(8, "Parse8")]
    public async Task NoType_InvalidParserMethodSignature_GQL018(int idx, string methodName)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[Parser(nameof({{methodName}}))]|}
                  public string Hello { get; set; }

                  private object Parse1(object value) => value; // not static
                  private static string Parse2(object value) => "value"; // wrong return type
                  private static object Parse3(string value) => value; // wrong parameter type
                  private static object Parse4(object value1, object value2) => value1; // wrong second parameter type
                  private static object Parse5() => "value"; // no parameters
                  private static object Parse6(object value, string converter) => value; // wrong second parameter type
                  private static object Parse7(string value, IValueConverter converter) => value; // wrong first parameter type
                  private static object Parse8(object value, IValueConverter converter, object extra) => value; // too many parameters
              }
              """;

        var expected = VerifyCS.Diagnostic(ParserAttributeAnalyzer.ParserMethodMustBeValid)
            .WithLocation(0).WithArguments(methodName, "");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "internal")]
    [InlineData(2, "protected")]
    [InlineData(3, "private")]
    [InlineData(4, "")]
    public async Task OtherType_NonPublicMethod_GQL018(int idx, string accessor)
    {
        _ = idx;
        string source =
            $$"""
              using GraphQL;

              namespace Sample.Server;

              public class TestClass
              {
                  {|#0:[Parser(typeof(ParserClass))]|}
                  public string Hello { get; set; }
              }

              public class ParserClass
              {
                  {{accessor}} static object Parse(object value) => value;
              }
              """;

        var expected = VerifyCS.Diagnostic(ParserAttributeAnalyzer.ParserMethodMustBeValid)
            .WithLocation(0).WithArguments("Parse", "public ");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(1, "TestClass", "[Parser(\"Parse\")]")]
    [InlineData(2, "TestClass", "[Parser(typeof(TestClass))]")]
    [InlineData(3, "TestClass", "[Parser(typeof(TestClass), \"Parse\")]")]
    [InlineData(4, "ParserClass", "[Parser(typeof(ParserClass))]")]
    [InlineData(5, "ParserClass", "[Parser(typeof(ParserClass), \"Parse\")]")]
    public async Task MethodNotFound_GQL017(int idx, string parserType, string attribute)
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

                  private static object ParseValue(object value) => value;
              }

              public class ParserClass
              {
                  public static object ParseValue(object value) => value;
              }
              """;

        var expected = VerifyCS.Diagnostic(ParserValidatorAttributeAnalyzer.CouldNotFindMethod)
            .WithLocation(0).WithArguments("Parse", parserType);
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
