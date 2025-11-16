using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLFieldArgumentTests
{
    [Fact]
    public async Task TryCreate_NotArgumentInvocation_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    var x = SomeMethod();
                }

                private string SomeMethod() => "test";
            }
            """);

        var invocation = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "SomeMethod");

        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldBeNull();
    }

    [Theory]
    [InlineData("GetCustomBuilder().Argument<IntGraphType>(\"id\")")]
    [InlineData("this.Argument<IntGraphType>(\"id\")")]
    [InlineData("Argument<IntGraphType>(\"id\")")]
    public async Task TryCreate_ArgumentOnNonGraphQLMethod_ReturnsNull(string methodInvocation)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    {{methodInvocation}};
                }

                private CustomBuilder GetCustomBuilder() => new CustomBuilder();

                private void Argument<T>(string name) => { };
            }

            public class CustomBuilder
            {
                public CustomBuilder Argument<T>(string name) => this;
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        // Should return null because CustomBuilder.Argument is not from GraphQL library
        argument.ShouldBeNull();
    }

    [Fact]
    public async Task TryCreate_ArgumentInvocation_ReturnsInstance()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Syntax.ShouldBe(invocation);
        argument.SemanticModel.ShouldBe(context.SemanticModel);
        await VerifyLocationAsync(context, argument.Location, "Argument<IntGraphType>(\"limit\")");
    }

    [Theory]
    [InlineData(".Argument<IntGraphType>(\"limit\")", "limit")]
    [InlineData(".Argument<IntGraphType>(name: \"limit\")", "limit")]
    public async Task Name_ExplicitStringLiteral_ReturnsNameWithLocation(string argumentCall, string expectedName)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        {{argumentCall}};
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Name.ShouldNotBeNull();
        argument.Name.Value.ShouldBe(expectedName);
        argument.Name.Location.ShouldNotBeNull();
        await VerifyLocationAsync(context, argument.Name.Location, $"\"{expectedName}\"");
    }

    [Fact]
    public async Task Name_ConstFieldReference_ReturnsNameWithLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                private const string ArgName = "limit";

                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>(ArgName);
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Name.ShouldNotBeNull();
        argument.Name.Value.ShouldBe("limit");
        await VerifyLocationAsync(context, argument.Name.Location, "ArgName");
    }

    [Fact]
    public async Task GraphType_GenericTypeArgument_ReturnsTypeWithLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.GraphType.ShouldNotBeNull();
        argument.GraphType.Value?.Name.ShouldBe("IntGraphType");
        argument.GraphType.Location.ShouldNotBeNull();
        await VerifyLocationAsync(context, argument.GraphType.Location, "IntGraphType");
    }

    [Fact]
    public async Task Description_ArgumentWithDescription_ReturnsDescriptionWithLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit", "Maximum number of items");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Description.ShouldNotBeNull();
        argument.Description.Value.ShouldBe("Maximum number of items");
        await VerifyLocationAsync(context, argument.Description.Location, "\"Maximum number of items\"");
    }

    [Fact]
    public async Task Description_ConfigureAction_ReturnsDescriptionWithLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit", arg => arg.Description = "Maximum number of items");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Description.ShouldNotBeNull();
        argument.Description.Value.ShouldBe("Maximum number of items");
        await VerifyLocationAsync(context, argument.Description.Location, "\"Maximum number of items\"");
    }

    [Theory]
    [InlineData("10", 10)]
    [InlineData("true", true)]
    [InlineData("\"default\"", "default")]
    [InlineData("null", null)]
    [InlineData("DefaultLimitConst", 100)]
    public async Task DefaultValue_ConfigureAction_ReturnsValueWithLocation(string defaultValueExpr, object? expectedDefaultValue)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                private const int DefaultLimitConst = 100;

                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit", arg => arg.DefaultValue = {{defaultValueExpr}});
                }

                private string GetDefault() => "default";
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.DefaultValue.ShouldNotBeNull();
        argument.DefaultValue.Value.ShouldBe(expectedDefaultValue);
        await VerifyLocationAsync(context, argument.DefaultValue.Location, defaultValueExpr);
    }

    [Fact]
    public async Task DefaultValue_ConfigureAction_FromMethod_ReturnNull()
    {
        var context = await TestContext.CreateAsync(
            """
              using GraphQL.Types;

              namespace Sample;

              public class MyType : ObjectGraphType
              {
                  public MyType()
                  {
                      Field<StringGraphType>("name")
                          .Argument<IntGraphType>("limit", arg => arg.DefaultValue = GetDefault());
                  }

                  private string GetDefault() => "default";
              }
              """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.DefaultValue.ShouldBeNull(); // handling method invocation is not currently supported
    }

    [Fact]
    public async Task Argument_WithTypeInstance_ReturnsGraphType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument(typeof(IntGraphType), "limit");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);

        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Name.ShouldNotBeNull();
        argument.Name.Value.ShouldBe("limit");
        argument.GraphType.ShouldNotBeNull(); // TODO: improve test for GraphType
        await VerifyLocationAsync(context, argument.Location, "Argument(typeof(IntGraphType), \"limit\")");
    }

    [Fact]
    public async Task Argument_WithCLRType_ReturnsName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<int>("limit");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);

        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Name.ShouldNotBeNull();
        argument.Name.Value.ShouldBe("limit");
    }

    [Fact]
    public async Task Argument_ConfigureWithMultipleProperties_ExtractsAll()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>("limit", arg =>
                        {
                            arg.Description = "Maximum items";
                            arg.DefaultValue = 50;
                        });
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);

        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.Name.ShouldNotBeNull();
        argument.Name.Value.ShouldBe("limit");
        argument.Description.ShouldNotBeNull();
        argument.Description.Value.ShouldBe("Maximum items");
        argument.DefaultValue.ShouldNotBeNull();
        argument.DefaultValue.Value.ShouldBe(50);
    }

    [Fact]
    public async Task Location_Multiline_ReturnsCorrectLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name")
                        .Argument<IntGraphType>(
                            "limit"
                        );
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);

        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        await VerifyLocationAsync(context, argument.Location,
            """
            Argument<IntGraphType>(
                            "limit"
                        )
            """);
    }

    [Fact]
    public async Task ParentField_ChainedFromField_ReturnsParentField()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("user")
                        .Argument<IntGraphType>("id");
                }
            }
            """);

        var invocation = FindArgumentInvocation(context.Root);
        var argument = GraphQLFieldArgument.TryCreate(invocation, context.SemanticModel);

        argument.ShouldNotBeNull();
        argument.ParentField.ShouldNotBeNull();
        argument.ParentField.Name.ShouldNotBeNull();
        argument.ParentField.Name.Value.ShouldBe("user");
    }

    [Fact]
    public async Task ParentField_MultipleArguments_AllHaveSameParent()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("user")
                        .Argument<IntGraphType>("id")
                        .Argument<StringGraphType>("name");
                }
            }
            """);

        var argumentInvocations = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => GetMethodName(i) == "Argument")
            .ToList();

        argumentInvocations.Count.ShouldBe(2);

        var arg1 = GraphQLFieldArgument.TryCreate(argumentInvocations[0], context.SemanticModel);
        var arg2 = GraphQLFieldArgument.TryCreate(argumentInvocations[1], context.SemanticModel);

        arg1.ShouldNotBeNull();
        arg2.ShouldNotBeNull();

        arg1.ParentField.ShouldNotBeNull();
        arg2.ParentField.ShouldNotBeNull();

        arg1.ParentField.Name.ShouldNotBeNull().Value.ShouldBe("user");
        arg2.ParentField.Name.ShouldNotBeNull().Value.ShouldBe("user");

        // Verify they reference the same field instance
        arg1.ParentField.Syntax.ShouldBe(arg2.ParentField.Syntax);
    }

    /// <summary>
    /// Finds the first Argument() invocation in the syntax tree.
    /// </summary>
    private static InvocationExpressionSyntax FindArgumentInvocation(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "Argument");
    }

    /// <summary>
    /// Extracts the method name from an invocation expression syntax.
    /// </summary>
    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax genericName => genericName.Identifier.Text,
            _ => null
        };
    }

    private static async Task VerifyLocationAsync(TestContext context, Location? location, string expectedLocationText)
    {
        location.ShouldNotBeNull();
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(location.SourceSpan);
        locationText.ShouldBe(expectedLocationText);
    }
}
