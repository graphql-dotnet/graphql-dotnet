using GraphQL.Analyzers.SDK;

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

        var classDeclaration = context.Root.FindClassDeclaration("Product");
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
                    Field<StringGraphType>("sku");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.FindClassDeclaration("Product");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        graphType.FederationKeys[0].FieldsString.ShouldBe("id");
        graphType.FederationKeys[0].GraphType.ShouldBe(graphType);
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

        var classDeclaration = context.Root.FindClassDeclaration("Product");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(2);

        graphType.FederationKeys[0].FieldsString.ShouldBe("id");
        graphType.FederationKeys[1].FieldsString.ShouldBe("sku");
    }

    [Theory]
    [InlineData("\"id\"", true)]
    [InlineData("\"id\", true", true)]
    [InlineData("\"id\", false", false)]
    [InlineData("\"id\", resolvable: true", true)]
    [InlineData("\"id\", resolvable: false", false)]
    [InlineData("resolvable: true, fields: \"id\"", true)]
    [InlineData("resolvable: false, fields: \"id\"", false)]
    public async Task FederationKey_Resolvable_ShouldBeCorrect(string keyArgs, bool resolvable)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key({{keyArgs}});
                    Field<StringGraphType>("id");
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.FindClassDeclaration("Product");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.Resolvable.ShouldBe(resolvable);
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

        var classDeclaration = context.Root.FindClassDeclaration("Product");
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
    public async Task FederationKey_GetFieldLocation_ReturnsCorrectSpan()
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
                }
            }
            """);

        var classDeclaration = context.Root.FindClassDeclaration("Product");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        var key = graphType.FederationKeys[0];

        var sourceText = await context.SyntaxTree.GetTextAsync();

        var idLocation = key.GetFieldLocation("id");
        var idText = sourceText.ToString(idLocation.SourceSpan);
        idText.ShouldBe("id");

        var skuLocation = key.GetFieldLocation("sku");
        var skuText = sourceText.ToString(skuLocation.SourceSpan);
        skuText.ShouldBe("sku");
    }

    [Fact]
    public async Task FederationKey_InvalidKeyFields_StoredWithNullFields()
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

        var classDeclaration = context.Root.FindClassDeclaration("Product");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        // Invalid keys should be stored but with null Fields property
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);
        var key = graphType.FederationKeys[0];
        key.Fields.ShouldBeNull();
        key.FieldsString.ShouldNotBeNull();
    }
}
