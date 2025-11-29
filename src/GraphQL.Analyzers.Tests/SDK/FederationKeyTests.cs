using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class FederationKeyTests
{
    [Fact]
    public async Task GraphQLGraphType_NoFederationKeys_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldBeNull();
    }

    [Fact]
    public async Task GraphQLGraphType_SingleKeyDirective_ReturnsKey()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key("id");
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe("id");
        key.Resolvable.ShouldBeTrue();
        key.GraphType.ShouldBe(graphType);
        key.Fields.ShouldNotBeNull();
        key.GetFieldNames().ShouldBe(["id"]);
    }

    [Fact]
    public async Task GraphQLGraphType_MultipleFieldsInKey_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key("id sku");
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("sku");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe("id sku");
        key.GetFieldNames().ShouldBe(["id", "sku"]);
        key.IncludesField("id").ShouldBeTrue();
        key.IncludesField("sku").ShouldBeTrue();
        key.IncludesField("name").ShouldBeFalse();
    }

    [Fact]
    public async Task GraphQLGraphType_ArrayOfFields_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key(new[] { "id", "sku" });
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("sku");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe("id sku");
        key.GetFieldNames().ShouldBe(["id", "sku"]);
    }

    [Fact]
    public async Task GraphQLGraphType_KeyWithResolvableFalse_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key("id", resolvable: false);
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe("id");
        key.Resolvable.ShouldBeFalse();
    }

    [Fact]
    public async Task GraphQLGraphType_MultipleKeyDirectives_ReturnsAllKeys()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key("id");
                    this.Key("sku");
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("sku");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(2);

        graphType.FederationKeys[0].FieldsString.ShouldBe("id");
        graphType.FederationKeys[1].FieldsString.ShouldBe("sku");
    }

    [Fact]
    public async Task GraphQLGraphType_CompositeKey_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Review : ObjectGraphType
            {
                public Review()
                {
                    this.Key("product { id } author { id }");
                    Field<StringGraphType>("id");
                    Field<ProductGraphType>("product");
                    Field<AuthorGraphType>("author");
                }
            }

            public class ProductGraphType : ObjectGraphType
            {
                public ProductGraphType()
                {
                    Field<StringGraphType>("id");
                }
            }

            public class AuthorGraphType : ObjectGraphType
            {
                public AuthorGraphType()
                {
                    Field<StringGraphType>("id");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Review");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe("product { id } author { id }");
        key.GetFieldNames().ShouldBe(["product", "author"]);
        key.IncludesField("product").ShouldBeTrue();
        key.IncludesField("author").ShouldBeTrue();
    }

    [Fact]
    public async Task FederationKey_Location_PointsToKeyInvocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key("id");
                    Field<StringGraphType>("id");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.Location.ShouldNotBeNull();
        key.Location.ShouldNotBe(Microsoft.CodeAnalysis.Location.None);

        // Verify location points to the Key invocation
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(key.Location.SourceSpan);
        locationText.ShouldContain("Key");
        locationText.ShouldContain("id");
    }

    [Fact]
    public async Task GraphQLGraphType_InvalidKeyFields_IgnoresKey()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    // Invalid GraphQL selection syntax
                    this.Key("id {");
                    Field<StringGraphType>("id");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Product");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        // Invalid keys should be ignored during analysis
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);
        var key = graphType.FederationKeys[0];
        key.Fields.ShouldBeNull();
        key.FieldsString.ShouldNotBeNull();
    }
}
