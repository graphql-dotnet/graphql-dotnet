using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLFieldGraphTypeTests
{
    [Fact]
    public async Task GraphType_NonNullStringGraphType_IsNullableFalse()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<NonNullGraphType<StringGraphType>>("name");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.IsNullable.ShouldBeFalse();
        field.GraphType.IsList.ShouldBeFalse();
        field.GraphType.TypeSymbol.Name.ShouldBe("NonNullGraphType");
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("StringGraphType");
    }

    [Fact]
    public async Task GraphType_NullableStringGraphType_IsNullableTrue()
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
        field.GraphType.IsNullable.ShouldBeTrue();
        field.GraphType.IsList.ShouldBeFalse();
        field.GraphType.TypeSymbol.Name.ShouldBe("StringGraphType");
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("StringGraphType");
    }

    [Fact]
    public async Task GraphType_ListOfNonNullString_IsListTrueIsNullableTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<ListGraphType<NonNullGraphType<StringGraphType>>>("names");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.IsNullable.ShouldBeTrue(); // The list itself is nullable
        field.GraphType.IsList.ShouldBeTrue();
        field.GraphType.TypeSymbol.Name.ShouldBe("ListGraphType");
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("StringGraphType");
    }

    [Fact]
    public async Task GraphType_NonNullListOfNonNullString_IsListTrueIsNullableFalse()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("names");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.IsNullable.ShouldBeFalse(); // The list itself is non-nullable
        field.GraphType.IsList.ShouldBeTrue();
        field.GraphType.TypeSymbol.Name.ShouldBe("NonNullGraphType");
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("StringGraphType");
    }

    [Fact]
    public async Task GraphType_ListOfNullableString_IsListTrueIsNullableTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<ListGraphType<StringGraphType>>("names");
                }
            }
            """);

        var invocation = FindFieldInvocation(context.Root);
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.IsNullable.ShouldBeTrue();
        field.GraphType.IsList.ShouldBeTrue();
        field.GraphType.TypeSymbol.Name.ShouldBe("ListGraphType");
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("StringGraphType");
    }

    [Fact]
    public async Task GetUnwrappedType_CustomGraphType_ReturnsGraphType()
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

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<NonNullGraphType<ListGraphType<PersonGraphType>>>("people");
                }
            }
            """);

        var invocation = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "Field" && i.ToString().Contains("people"));

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        field.ShouldNotBeNull();
        field.GraphType.ShouldNotBeNull();
        field.GraphType.IsNullable.ShouldBeFalse();
        field.GraphType.IsList.ShouldBeTrue();
        field.GraphType.UnwrappedTypeSymbol.Name.ShouldBe("PersonGraphType");

        // GetUnwrappedType should return the PersonGraphType instance
        var unwrappedType = field.GraphType.GetUnwrappedType();
        unwrappedType.ShouldNotBeNull();
        unwrappedType.Name.ShouldBe("PersonGraphType");
    }

    [Fact]
    public async Task GetUnwrappedType_ScalarGraphType_ReturnsNull()
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

        // GetUnwrappedType should return null for scalar types
        // since StringGraphType is not declared in the same compilation
        var unwrappedType = field.GraphType.GetUnwrappedType();
        unwrappedType.ShouldBeNull();
    }

    private static InvocationExpressionSyntax FindFieldInvocation(Microsoft.CodeAnalysis.SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => GetMethodName(i) == "Field");
    }

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
}
