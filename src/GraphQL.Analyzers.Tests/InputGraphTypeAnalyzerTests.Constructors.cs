using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpAnalyzerVerifier<
    GraphQL.Analyzers.InputGraphTypeAnalyzer>;

namespace GraphQL.Analyzers.Tests;

public partial class InputGraphTypeAnalyzerTests
{
    [Theory]
    [InlineData(null, false)] // implicit default constructor
    [InlineData("public MySource() { }", false)]
    [InlineData("private MySource() { }", true)]
    [InlineData("internal MySource() { }", true)]
    [InlineData("public MySource(string name) { }", false)]
    [InlineData("private MySource(string name) { }", true)]
    [InlineData("internal MySource(string name) { }", true)]
    public async Task SingleConstructor_GQL010_WhenNonPublic(string constructor, bool report)
    {
        string source =
            $$"""
              using GraphQL.Types;
              using System.Collections.Generic;

              namespace Sample.Server;

              public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
              {
                  public CustomInputObjectGraphType()
                  {
                      Field<StringGraphType>("Name");
                  }
              }

              public class MySource
              {
                  {{constructor}}
                  public string Name { get; set; }
              }
              """;

        var expected = report
            ? new[]
            {
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
                    .WithSpan(6, 64, 6, 72).WithArguments("MySource")
            }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DefaultAndNonDefaultConstructor_NoDiagnostics()
    {
        const string source =
            """
              using GraphQL.Types;
              using System.Collections.Generic;

              namespace Sample.Server;

              public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
              {
                  public CustomInputObjectGraphType()
                  {
                      Field<StringGraphType>("Name");
                  }
              }

              public class MySource
              {
                  public MySource() { }
                  public MySource(string name) { }
                  public string Name { get; set; }
              }
              """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TwoNonDefaultConstructors_GQL010()
    {
        const string source =
            """
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class MySource
            {
                public MySource(int num) { }
                public MySource(string name) { }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
            .WithSpan(6, 64, 6, 72).WithArguments("MySource");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("[GraphQLConstructor]", null, false)]
    [InlineData(null, "[GraphQLConstructor]", false)]
    [InlineData("[GraphQLConstructor]", "[GraphQLConstructor]", true)]
    [InlineData("[Another]", "[GraphQLConstructor]", false)]
    [InlineData("[Another]", "[Another]", true)]
    public async Task TwoNonDefaultConstructors_WithAttribute_GQL010_WhenNotSingleGraphQLConstructorAttribute(string attribute1, string attribute2, bool report)
    {
        string source =
            $$"""
              using System;
              using GraphQL;
              using GraphQL.Types;
              using System.Collections.Generic;

              namespace Sample.Server;

              public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
              {
                  public CustomInputObjectGraphType()
                  {
                      Field<StringGraphType>("Name");
                  }
              }

              public class MySource
              {
                  {{attribute1}}
                  public MySource(int num) { }
                  {{attribute2}}
                  public MySource(string name) { }
                  public string Name { get; set; }
              }

              [AttributeUsage(AttributeTargets.Constructor)]
              public class AnotherAttribute : Attribute { }
              """;

        var expected = report
            ? new[]
            {
                VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
                    .WithSpan(8, 64, 8, 72).WithArguments("MySource")
            }
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DefaultConstructor_SourceTypeIsAbstract_GQL010()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : InputObjectGraphType<MySource>
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public abstract class MySource
            {
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
            .WithSpan(7, 64, 7, 72).WithArguments("MySource");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task TwoNonDefaultConstructors_SourceTypeIsOpenGeneric_NoDiagnostics()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType<TSource> : InputObjectGraphType<TSource>
                where TSource : MySource
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class MySource
            {
                public MySource(int num) { }
                public MySource(string name) { }
                public string Name { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task TwoNonDefaultConstructors_SourceTypeDefinedByTheBaseClass_GQL010_OnBaseOnly()
    {
        const string source =
            """
            using GraphQL;
            using GraphQL.Types;
            using System.Collections.Generic;

            namespace Sample.Server;

            public class CustomInputObjectGraphType : BaseInputObjectGraphType
            {
                public CustomInputObjectGraphType()
                {
                    Field<StringGraphType>("Name");
                }
            }

            public class BaseInputObjectGraphType : InputObjectGraphType<MySource>
            {
            }

            public class MySource
            {
                public MySource(int num) { }
                public MySource(string name) { }
                public string Name { get; set; }
            }
            """;

        var expected = VerifyCS.Diagnostic(InputGraphTypeAnalyzer.CanNotResolveInputSourceTypeConstructor)
            .WithSpan(15, 62, 15, 70).WithArguments("MySource");

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
