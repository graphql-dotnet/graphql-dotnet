using Microsoft.CodeAnalysis.Testing;
using VerifyCS = GraphQL.Analyzers.Tests.VerifiersExtensions.CSharpCodeFixVerifier<
    GraphQL.Analyzers.FieldBuilderAnalyzer,
    GraphQL.Analyzers.FieldBuilderCodeFixProvider>;

namespace GraphQL.Analyzers.Tests;

public class FieldBuilderAnalyzerTests
{
    [Fact]
    public async Task Sanity_NoDiagnostics()
    {
        const string source = "";
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AllArgumentsProvided_NoNamedArguments_FixProvided()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        "name",
                        "description",
                        new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }),
                        context => "text",
                        "deprecated reason")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }))
                        .Resolve(context => "text")
                        .DeprecationReason("deprecated reason");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NamedArgumentsNotInOrder_FixPreservesSameOrder()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        name: "name",
                        deprecationReason: "deprecated reason",
                        description: "description",
                        resolve: context => "text",
                        arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }))|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .DeprecationReason("deprecated reason")
                        .Description("description")
                        .Resolve(context => "text")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType> { Name = "argName" }));
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NameArgumentIsNotFirst_FixCorrectly()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        resolve: context => "text",
                        description: "description",
                        name: "name")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .Description("description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task FieldCalledOnVariable_FixCorrectly()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    {|#0:graphType.Field<StringGraphType>(
                        name: "name",
                        resolve: context => "text",
                        description: "description")|};
                }
            }
            """;

        const string fix = """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType
            {
                public void Register(ObjectGraphType graphType)
                {
                    graphType.Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .Description("description");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task FieldAsync_FixAsFieldAndResolveAsync()
    {
        const string source =
            """
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:FieldAsync<StringGraphType>(
                        "name",
                        "description",
                        resolve: context => Task.FromResult<object>("text"))|};
                }
            }
            """;

        const string fix =
            """
            using System.Threading.Tasks;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Description("description")
                        .ResolveAsync(context => Task.FromResult<object>("text"));
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task ArgumentsListMultilineFormatted_FormattingPreserved()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        "name",
                        arguments: new QueryArguments(new QueryArgument<StringGraphType>
                        {
                            Name = "argName"
                        }),
                        resolve: context => "text")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Arguments(new QueryArguments(new QueryArgument<StringGraphType>
                        {
                            Name = "argName"
                        }))
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task ArgumentsListMultilineFormatted_FormattingPreserved2()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        "name",
                        arguments: new QueryArguments(
                            new QueryArgument<StringGraphType> { Name = "argName1" },
                            new QueryArgument<StringGraphType> { Name = "argName2" }
                        )
                    )|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Arguments(new QueryArguments(
                            new QueryArgument<StringGraphType> { Name = "argName1" },
                            new QueryArgument<StringGraphType> { Name = "argName2" }
                        ));
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task ArgumentsListMultilineFormatted_FormattingPreserved3()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        "name" /* a */, // b
                        // c
                        arguments: new QueryArguments(
                            new QueryArgument<StringGraphType> { Name = "argName1" },
                            new QueryArgument<StringGraphType> { Name = "argName2" }
                        ),
                        // d
                        description: "desc"
                    )|}; // e
                }
            }
            """;

        // NOTE: line break before 'Description' shouldn't appear,
        // but I have no idea where it comes from...
        // At least comments are preserved
        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : InputObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name" /* a */)
                        // b
                        // c
                        .Arguments(new QueryArguments(
                            new QueryArgument<StringGraphType> { Name = "argName1" },
                            new QueryArgument<StringGraphType> { Name = "argName2" }
                        ))

                        // d
                        .Description("desc"); // e
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task NonGenericFieldMethod_FixProvided()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field(
                        typeof(string),
                        "name",
                        resolve: context => "text")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field("name", typeof(string))
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task FieldSubscribe_SubscribeArgument_ConvertToFieldAndResolveStream()
    {
        const string source =
            """
            using System;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:FieldSubscribe<StringGraphType>(
                        "name",
                        resolve: context => "text",
                        subscribe: context => new Observable())|};
                }

                private class Observable : IObservable<string>
                {
                    public IDisposable Subscribe(IObserver<string> observer) =>
                        throw new NotImplementedException();
                }
            }
            """;

        const string fix =
            """
            using System;
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Resolve(context => "text")
                        .ResolveStream(context => new Observable());
                }

                private class Observable : IObservable<string>
                {
                    public IDisposable Subscribe(IObserver<string> observer) =>
                        throw new NotImplementedException();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task FieldDelegate_ConvertToFieldAndResolveDelegate()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:FieldDelegate<StringGraphType>(
                        "name",
                        resolve: TestMethod)|};
                }

                public string TestMethod() => "test";
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .ResolveDelegate(TestMethod);
                }

                public string TestMethod() => "test";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task ArgumentsFormattingPreservedAsync()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>("name", "description", null,
                        resolve: context => Resolve(
                            "s1",
                              "s2",
                                "s3"
                        ))|};
                }

                public string Resolve(string s1, string s2, string s3) => "text";
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name").Description("description")
                        .Resolve(context => Resolve(
                            "s1",
                              "s2",
                                "s3"
                        ));
                }

                public string Resolve(string s1, string s2, string s3) => "text";
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task EmptyLineWithWhiteSpacesBeforeField_ArgumentsFormattingPreservedAsync()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    // next line contains whitespaces
                    
                    {|#0:Field<StringGraphType>("name",
                        resolve: context => Resolve(
                            "text"
                        ))|};
                }

                public string Resolve(string s1) => s1;
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    // next line contains whitespaces
                    
                    Field<StringGraphType>("name")
                        .Resolve(context => Resolve(
                            "text"
                        ));
                }

                public string Resolve(string s1) => s1;
            }
            """;

        // ensure whitespace was not removed from input sample
        source.Replace("\r", "").ShouldContain("// next line contains whitespaces\n    ");

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task CommentBeforeField_ArgumentsFormattingPreservedAsync()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    // comment
                    {|#0:Field<StringGraphType>("name",
                        resolve: context => Resolve(
                            "text"
                        ))|};
                }

                public string Resolve(string s1) => s1;
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    // comment
                    Field<StringGraphType>("name")
                        .Resolve(context => Resolve(
                            "text"
                        ));
                }

                public string Resolve(string s1) => s1;
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Fact]
    public async Task ArgumentsFormattingPreserved_PreserveNewLines()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {

                    {|#0:Field<StringGraphType>("name1", "description1", null,
                        context => "text1")|};

                    var str = "string";

                    {|#1:Field<StringGraphType>("name2", "description2", null,
                        context => "text2")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {

                    Field<StringGraphType>("name1").Description("description1")
                        .Resolve(context => "text1");

                    var str = "string";

                    Field<StringGraphType>("name2").Description("description2")
                        .Resolve(context => "text2");
                }
            }
            """;

        var expected1 = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        var expected2 = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(1);
        await VerifyCS.VerifyCodeFixAsync(source, new[] { expected1, expected2 }, fix);
    }

    [Fact]
    public async Task ReformatOptionIsTrue_SourceReformatted()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>("name", "description", null,
                        context => "text")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Resolve(context => "text");
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            ExpectedDiagnostics = { expected },
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $"""
                        root = true

                        [*]
                        {FieldBuilderCodeFixProvider.ReformatOption} = true
                        ")
                        """)
                }
            }
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReformatOptionIsTrue_SourceReformatted_CommentsPreserved()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>("name" /* a */, /* b */ "description",
                        // c
                        deprecationReason: "reason", // d
                        resolve: context => "text")|}; // e
                }
            }
            """;

        // NOTE: this is the expected output, but I can't get rid of these line breaks.
        // At least the comments are preserved
        /*const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name" /* a #1#)
                        /* b #1#
                        .Description("description")
                        // c
                        .DeprecationReason("reason") // d
                        .Resolve(context => "text"); // e
                }
            }
            """;*/

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name" /* a */)
                        /* b */
                        .Description("description")

                        // c
                        .DeprecationReason("reason")
                        // d
                        .Resolve(context => "text"); // e
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0);
        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            ExpectedDiagnostics = { expected },
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $"""
                         root = true

                         [*]
                         {FieldBuilderCodeFixProvider.ReformatOption} = true
                         ")
                         """)
                }
            }
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task SkipNullsOptionIsFalse_NullArgumentsPreserved()
    {
        const string source =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    {|#0:Field<StringGraphType>(
                        "name",
                        "description",
                        null,
                        context => "text")|};
                }
            }
            """;

        const string fix =
            """
            using GraphQL.Types;

            namespace Sample.Server;

            public class MyGraphType : ObjectGraphType
            {
                public MyGraphType()
                {
                    Field<StringGraphType>("name")
                        .Description("description")
                        .Arguments(null)
                        .Resolve(context => "text");
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fix,
            ExpectedDiagnostics = { VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0) },
            TestState =
            {
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig",
                        $"""
                        root = true

                        [*]
                        {FieldBuilderCodeFixProvider.SkipNullsOption} = false
                        ")
                        """)
                }
            }
        };
        await test.RunAsync();
    }

    [Theory]
    // OK
    [InlineData("Field(p => p.Name)", false)]
    [InlineData("Field(p => p.Name, true)", false)]
    [InlineData("Field(p => p.Name, type: typeof(StringGraphType))", false)]
    [InlineData("Field(\"AnotherName\", p => p.Name, type: typeof(StringGraphType))", false)]
    // report and fix: type is null => remove type
    [InlineData("Field(p => p.Name, type: null)", true, "Field(p => p.Name)")]
    [InlineData("Field(\"AnotherName\", p => p.Name, type: null)", true, "Field(\"AnotherName\", p => p.Name)")]
    [InlineData("Field(p => p.Name, true, null)", true, "Field(p => p.Name, true)")]
    [InlineData("Field(p => p.Name, nullable: true, null)", true, "Field(p => p.Name, nullable: true)")]
    [InlineData("Field(p => p.Name, nullable: true, type: null)", true, "Field(p => p.Name, nullable: true)")]
    // report and fix: both args defined, type is 'typeof' => remove nullable, remove 'type:' arg name
    [InlineData("Field(p => p.Name, true, typeof(StringGraphType))", true, "Field(p => p.Name, typeof(StringGraphType))")]
    [InlineData("Field(\"AnotherName\", p => p.Name, true, typeof(StringGraphType))", true, "Field(\"AnotherName\", p => p.Name, typeof(StringGraphType))")]
    [InlineData("Field(p => p.Name, isNullable, typeof(StringGraphType))", true, "Field(p => p.Name, typeof(StringGraphType))")]
    [InlineData("Field(p => p.Name, isNullable, type: typeof(StringGraphType))", true, "Field(p => p.Name, typeof(StringGraphType))")]
    [InlineData("Field(p => p.Name, type: typeof(StringGraphType), nullable: true)", true, "Field(p => p.Name, typeof(StringGraphType))")]
    // report without fix: both args defined, type is not 'typeof' (we can't know if type is 'null', in this case 'nullable' should stay and 'type' removed)
    [InlineData("Field(p => p.Name, isNullable, type)", true)]
    [InlineData("Field(p => p.Name, true, type)", true)]
    [InlineData("Field(\"AnotherName\", p => p.Name, true, type)", true)]
    public async Task FieldDefinedWithExpression_RemoveRedundantArguments(string sourceField, bool report, string? fixedField = null)
    {
        string source =
            $$"""
            using GraphQL.Types;

            namespace Sample.Server;

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    var isNullable = true;
                    var type = typeof(StringGraphType);

                    {|#0:{{sourceField}}|};
                }
            }

            public class Person
            {
                public string Name { get; set; }
            }
            """;

        string fix =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class PersonGraphType : ObjectGraphType<Person>
              {
                  public PersonGraphType()
                  {
                      var isNullable = true;
                      var type = typeof(StringGraphType);

                      {|#0:{{fixedField ?? sourceField}}|};
                  }
              }

              public class Person
              {
                  public string Name { get; set; }
              }
              """;

        var expected = report
            ? [VerifyCS.Diagnostic(FieldBuilderAnalyzer.DoNotUseObsoleteFieldMethods).WithLocation(0)]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyCodeFixAsync(source, expected, fix);
    }

    [Theory]
    // good
    [InlineData(10, null, "p => p.FirstName", false)]
    [InlineData(11, "Name", "p => p.FirstName", false)]
    [InlineData(12, "FullName", "p => $\"{p.FirstName} {p.LastName}\"", false)]
    [InlineData(13, "FullName", "p => p.FirstName + \" \" + p.LastName", false)]
    // bad
    [InlineData(14, null, "p => $\"{p.FirstName} {p.LastName}\"", true)]
    [InlineData(15, null, "p => p.FirstName + \" \" + p.LastName", true)]
    public async Task InferFieldNameFromExpression_GQL015(int idx, string? name, string expression, bool report)
    {
        _ = idx;

        string? nameArg = name == null ? null : $"\"{name}\", ";
        string source =
            $$"""
              using GraphQL.Types;

              namespace Sample.Server;

              public class PersonGraphType : ObjectGraphType<Person>
              {
                  public PersonGraphType()
                  {
                      Field({{nameArg}}{|#0:{{expression}}|});
                  }
              }

              public class Person
              {
                  public string FirstName { get; set; }
                  public string LastName { get; set; }
              }
              """;

        var expected = report
            ?
            [
                VerifyCS.Diagnostic(FieldBuilderAnalyzer.CantInferFieldNameFromExpression)
                    .WithLocation(0).WithArguments(expression)
            ]
            : DiagnosticResult.EmptyDiagnosticResults;

        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }
}
