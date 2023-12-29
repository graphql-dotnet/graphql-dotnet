using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.NotAGraphTypeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public class NotAGraphTypeAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ObjectGraphType_NonGraphTypeArguments_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Builders;
            using GraphQL.DataLoader;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyClass
            {
                public MyClass()
                {
                    _ = typeof(ObjectGraphType<string>);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("AutoRegisteringInputObjectGraphType")]
    [InlineData("AutoRegisteringInterfaceGraphType")]
    [InlineData("AutoRegisteringObjectGraphType")]
    [InlineData("AutoSchema", "TQueryClrType")]
    [InlineData("ComplexGraphType")]
    [InlineData("ConnectionBuilder")]
    [InlineData("GraphQLClrInputTypeReference", "T")]
    [InlineData("GraphQLClrOutputTypeReference", "T")]
    [InlineData("IDataLoaderResult", "T")]
    [InlineData("InputObjectGraphType")]
    [InlineData("InterfaceGraphType", "TSource")]
    [InlineData("ObjectGraphType")]
    public async Task TypeWithSingleParameter_GraphTypeArguments_GQL011(string baseType, string typeParameterName = "TSourceType")
    {
        string source =
            $$"""
              using GraphQL.Builders;
              using GraphQL.DataLoader;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyClass
              {
                  public MyClass()
                  {
                      _ = typeof({{baseType}}<StringGraphType>);
                  }
              }
              """;

        int start = $"        _ = typeof({baseType}<".Length + 1;
        int end = start + "StringGraphType".Length;

        var expected = VerifyCS.Diagnostic().WithSpan(11, start, 11, end)
            .WithArguments("StringGraphType", typeParameterName, $"{baseType}<{typeParameterName}>");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData("ConnectionBuilder")]
    [InlineData("FieldBuilder")]
    public async Task TypeWithTwoParameters_GraphTypeArguments_GQL011(
        string baseType,
        string typeParameterName1 = "TSourceType",
        string typeParameterName2 = "TReturnType")
    {
        string source =
            $$"""
              using GraphQL.Builders;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyClass
              {
                  public MyClass()
                  {
                      _ = typeof({{baseType}}<StringGraphType, IntGraphType>);
                  }
              }
              """;

        int start1 = $"        _ = typeof({baseType}<".Length + 1;
        int end1 = start1 + "StringGraphType".Length;

        int start2 = end1 + ", ".Length;
        int end2 = start2 + "IntGraphType".Length;

        var expected = new[]
        {
            VerifyCS.Diagnostic().WithSpan(10, start1, 10, end1)
                .WithArguments("StringGraphType", typeParameterName1, $"{baseType}<{typeParameterName1}, {typeParameterName2}>"),
            VerifyCS.Diagnostic().WithSpan(10, start2, 10, end2)
                .WithArguments("IntGraphType", typeParameterName2, $"{baseType}<{typeParameterName1}, {typeParameterName2}>")
        };
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FieldBuilderArgument_GraphTypeArgument_GQL011()
    {
        const string source =
            """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name").Argument<IntGraphType>("arg", false);
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 49, 10, 61)
            .WithArguments("IntGraphType", "TArgumentClrType", "Argument<TArgumentClrType>");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FieldBuilderReturns_GraphTypeArgument_GQL011()
    {
        const string source =
            """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name").Returns<IntGraphType>();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 48, 10, 60)
            .WithArguments("IntGraphType", "TNewReturnType", "Returns<TNewReturnType>");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task FieldBuilderWithTwoTypeParameters_GraphTypeArgument_GQL011()
    {
        const string source =
            """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType, IntGraphType>("name");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 32, 10, 44)
            .WithArguments("IntGraphType", "TReturnType", "Field<TGraphType, TReturnType>");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ConnectionBuilder_Create_GraphTypeArgument_GQL011()
    {
        const string source =
            """
            using GraphQL.Builders;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    ConnectionBuilder.Create<StringGraphType, IntGraphType>();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic().WithSpan(10, 51, 10, 63)
            .WithArguments("IntGraphType", "TSourceType", "Create<TNodeType, TSourceType>");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TypeWithGenericTypeParameter_WithoutTypeConstraint_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType<TSource> : ObjectGraphType<TSource>
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("IDisposable", false)]
    [InlineData("IGraphType", true)]
    [InlineData("StringGraphType", true)]
    public async Task TypeWithGenericTypeParameter_WithGraphTypeConstraint_GQL011(string constraint, bool report)
    {
        string source =
            $$"""
              using System;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType<TSource> : ObjectGraphType<TSource>
                  where TSource : {{constraint}}
              {
              }
              """;

        var expected = report
            ? new[]
            {
                VerifyCS.Diagnostic().WithSpan(6, 53, 6, 60)
                    .WithArguments("TSource", "TSourceType", "ObjectGraphType<TSourceType>")
            }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TypeWithGenericTypeParameter_WithoutTypeConstraint_TypeParameterUsedInMethod_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL.Builders;

            namespace Sample.Server;

            public class MyGraphType<TSource, TReturn>
            {
                public void DoSomething() => FieldBuilder<TSource, TReturn>.Create();
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Theory]
    [InlineData("IDisposable", false)]
    [InlineData("IGraphType", true)]
    [InlineData("StringGraphType", true)]
    public async Task TypeWithGenericTypeParameter_WithTypeConstraint_TypeParameterUsedInMethod_GQL011(string constraint, bool report)
    {
        string source =
            $$"""
              using System;
              using GraphQL.Builders;
              using GraphQL.Types;

              namespace Sample.Server;

              public class MyGraphType<TSource, TReturn>
                    where TSource : {{constraint}}
              {
                  public void DoSomething() => FieldBuilder<TSource, TReturn>.Create();
              }
              """;

        var expected = report
            ? new[]
            {
                VerifyCS.Diagnostic().WithSpan(10, 47, 10, 54)
                    .WithArguments("TSource", "TSourceType", "FieldBuilder<TSourceType, TReturnType>")
            }
            : DiagnosticResult.EmptyDiagnosticResults;
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
