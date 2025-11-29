using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLFieldInvocationTests
{
    [Fact]
    public async Task TryCreate_NotFieldInvocation_ReturnsNull()
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

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);
        field.ShouldBeNull();
    }

    [Fact]
    public async Task TryCreate_NotGraphQLFieldInvocation_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field();
                }

                private string Field() => "test";
            }
            """);

        var invocation = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "Field");

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);
        field.ShouldBeNull();
    }

    [Theory]
    [InlineData("Field<StringGraphType>(\"name\")")]
    [InlineData("this.Field<StringGraphType>(\"name\")")]
    public async Task TryCreate_FieldInvocation_ReturnsInstance(string fieldInvocation)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    {{fieldInvocation}};
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Syntax.ShouldBe(invocation);
        field.SemanticModel.ShouldBe(context.SemanticModel);
    }

    [Theory]
    [InlineData("Field<StringGraphType>(\"firstName\")", "firstName")]
    [InlineData("Field<StringGraphType>(name: \"firstName\")", "firstName")]
    [InlineData("Field(type: typeof(StringGraphType), name: \"firstName\")", "firstName")]
    public async Task Name_ExplicitStringLiteral_ReturnsName(string fieldCall, string expectedName)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    {{fieldCall}};
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Name.ShouldNotBeNull();
        field.Name.Value.ShouldBe(expectedName);
        field.Name.Location.ShouldNotBeNull();

        await VerifyLocationAsync(context, field.Name.Location, $"\"{expectedName}\"");
    }

    [Fact]
    public async Task Name_ConstFieldReference_ReturnsName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                private const string FieldName = "firstName";

                public MyType()
                {
                    Field<StringGraphType>(FieldName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Name.ShouldNotBeNull();
        field.Name.Value.ShouldBe("firstName");

        await VerifyLocationAsync(context, field.Name.Location, "FieldName");
    }

    [Fact]
    public async Task Name_ExpressionBased_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Name.ShouldBeNull(); // Name property should be null for expression-based fields
    }

    [Fact]
    public async Task FieldExpression_ExpressionBased_ReturnsFieldExpression()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.Name.ShouldNotBeNull();
        field.FieldExpression.Name.Value.ShouldBe("FirstName");

        // Verify location points to the property name in the lambda
        await VerifyLocationAsync(context, field.FieldExpression.Name.Location, "FirstName");
    }

    [Fact]
    public async Task FieldExpression_IsValid_ValidPropertyAccess_ReturnsTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task FieldExpression_IsValid_ValidPropertyAccess_PropertiesReturnValues()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using System.ComponentModel;

            namespace Sample;

            public class Person
            {
                [Description("First name of the person")]
                [System.Obsolete("Use FullName")]
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.IsValid.ShouldBeTrue();

        // When IsValid is true, properties should return values
        field.FieldExpression.Name.ShouldNotBeNull();
        field.FieldExpression.Name.Value.ShouldBe("FirstName");

        field.FieldExpression.Description.ShouldNotBeNull();
        field.FieldExpression.Description.Value.ShouldBe("First name of the person");

        field.FieldExpression.DeprecationReason.ShouldNotBeNull();
        field.FieldExpression.DeprecationReason.Value.ShouldBe("Use FullName");
    }

    [Fact]
    public async Task FieldExpression_IsValid_ComplexExpression_FieldExpressionIsNotNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field("fullName", x => x.FirstName + " " + x.LastName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        // For complex expressions (not simple member access), FieldExpression should exist but IsValid should be false
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.IsValid.ShouldBeFalse();
        field.FieldExpression.Name.ShouldBeNull();
        field.FieldExpression.Description.ShouldBeNull();
        field.FieldExpression.DeprecationReason.ShouldBeNull();
    }

    [Fact]
    public async Task FieldExpression_IsValid_MethodCallExpression_FieldExpressionIsNotNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
                public string GetFullName() => FirstName;
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field("fullName", x => x.GetFullName());
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        // For method call expressions, FieldExpression should exist but IsValid should be false
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.IsValid.ShouldBeFalse();
        field.FieldExpression.Name.ShouldBeNull();
        field.FieldExpression.Description.ShouldBeNull();
        field.FieldExpression.DeprecationReason.ShouldBeNull();
    }

    [Fact]
    public async Task GetName_ExplicitName_ReturnsExplicitName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("firstName");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        var name = field.GetName();
        name.ShouldNotBeNull();
        name.Value.ShouldBe("firstName");
        await VerifyLocationAsync(context, name.Location, "\"firstName\"");
    }

    [Fact]
    public async Task GetName_ExpressionBased_ReturnsInferredName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field(x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        var name = field.GetName();
        name.ShouldNotBeNull();
        name.Value.ShouldBe("FirstName");

        // Verify location points to the property name in the lambda
        await VerifyLocationAsync(context, name.Location, "FirstName");
    }

    [Fact]
    public async Task GetName_ExplicitNameWithExpression_PrefersExplicitName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
            }

            public class MyType : ObjectGraphType<Person>
            {
                public MyType()
                {
                    Field("customName", x => x.FirstName);
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();

        // Name property should return the explicit name
        field.Name.ShouldNotBeNull();
        field.Name.Value.ShouldBe("customName");

        // FieldExpression should also be populated
        field.FieldExpression.ShouldNotBeNull();
        field.FieldExpression.Name.ShouldNotBeNull();
        field.FieldExpression.Name.Value.ShouldBe("FirstName");

        // GetName should prefer the explicit name
        var name = field.GetName();
        name.ShouldNotBeNull();
        name.Value.ShouldBe("customName");
        await VerifyLocationAsync(context, name.Location, "\"customName\"");
    }

    [Fact]
    public async Task GraphType_GenericTypeArgument_ReturnsType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.TypeSymbol.Name.ShouldBe("StringGraphType");
        field.GraphType.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task Description_ChainedMethod_ReturnsDescription()
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
                        .Description("A person's name");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Description.ShouldNotBeNull();
        field.Description.Value.ShouldBe("A person's name");

        await VerifyLocationAsync(context, field.Description.Location, "\"A person's name\"");
    }

    [Fact]
    public async Task DeprecationReason_ChainedMethod_ReturnsReason()
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
                        .DeprecationReason("Use fullName instead");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.DeprecationReason.ShouldNotBeNull();
        field.DeprecationReason.Value.ShouldBe("Use fullName instead");
    }

    [Fact]
    public async Task ResolverExpression_ResolveMethod_ReturnsExpression()
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
                        .Resolve(context => "value");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.ResolverExpression.ShouldNotBeNull();
        field.ResolverExpression.Value.ShouldBeOfType<SimpleLambdaExpressionSyntax>();
    }

    [Fact]
    public async Task Arguments_MultipleArguments_ReturnsAllArguments()
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
                        .Argument<IntGraphType>("limit")
                        .Argument<BooleanGraphType>("includeArchived");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.Arguments.Count.ShouldBe(2);
        field.Arguments[0].GetName()?.Value.ShouldBe("limit");
        field.Arguments[1].GetName()?.Value.ShouldBe("includeArchived");
    }

    [Fact]
    public async Task DeclaringGraphType_FieldInGraphType_ReturnsGraphType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field<StringGraphType>("name");
                }
            }

            public class Person
            {
                public string Name { get; set; }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.DeclaringGraphType.ShouldNotBeNull();
        field.DeclaringGraphType.Name.ShouldBe("PersonGraphType");
    }

    /// <summary>
    /// Finds the first Field() invocation in the syntax tree by checking the actual method name
    /// rather than using unreliable string matching.
    /// </summary>
    private static InvocationExpressionSyntax FindFieldInvocation(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "Field");
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

    private async Task VerifyLocationAsync(TestContext context, Location? location, string expectedLocationText)
    {
        location.ShouldNotBeNull();
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(location.SourceSpan);
        locationText.ShouldBe(expectedLocationText);
    }
}
