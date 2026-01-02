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

    [Fact]
    public async Task GraphQLGraphType_ConstFieldAsKeyValue_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                private const string IdField = "id";

                public Product()
                {
                    this.Key(IdField);
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
        key.GetFieldNames().ShouldBe(["id"]);
    }

    [Fact]
    public async Task GraphQLGraphType_ArrayWithConstFields_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                private const string IdField = "id";
                private const string SkuField = "sku";

                public Product()
                {
                    this.Key(new[] { IdField, SkuField });
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
    public async Task GraphQLGraphType_ArrayWithMixedConstAndLiteral_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                private const string IdField = "id";

                public Product()
                {
                    this.Key(new[] { IdField, "name" });
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
        key.FieldsString.ShouldBe("id name");
        key.GetFieldNames().ShouldBe(["id", "name"]);
    }

    [Fact]
    public async Task GraphQLGraphType_CollectionExpressionWithConstFields_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                private const string IdField = "id";
                private const string SkuField = "sku";

                public Product()
                {
                    this.Key([IdField, SkuField]);
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
    public async Task GraphQLGraphType_CollectionExpressionWithMixedConstAndLiteral_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class Product : ObjectGraphType
            {
                private const string IdField = "id";

                public Product()
                {
                    this.Key([IdField, "name"]);
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
        key.FieldsString.ShouldBe("id name");
        key.GetFieldNames().ShouldBe(["id", "name"]);
    }

    [Fact]
    public async Task GraphQLGraphType_NameofAsKeyValue_DoesNotParse()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
            }

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key(nameof(User.Id));
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
        key.FieldsString.ShouldBe("Id");
        key.GetFieldNames().ShouldBe(["Id"]);
    }

    [Theory]
    // single interpolation
    [InlineData("$\"{nameof(User.Id)}\"", "Id")]
    [InlineData("$\"{FieldName}\"", "Name")]
    // multiple interpolations
    [InlineData("$\"{nameof(User.Id)} {FieldName}\"", "Id", "Name")]
    // collection of interpolated strings
    [InlineData("[$\"{nameof(User.Id)}\", $\"{FieldName}\"]", "Id", "Name")]
    public async Task GraphQLGraphType_NameofStringInterpolationAsKeyValue_ParsesCorrectly(string interpolatedString, params string[] expectedKeys)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
            }

            public class Product : ObjectGraphType
            {
                private const string FieldName = "Name";
                public Product()
                {
                    this.Key({{interpolatedString}});
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
        key.FieldsString.ShouldBe(string.Join(' ', expectedKeys));
        key.GetFieldNames().ShouldBe(expectedKeys);
    }

    [Fact]
    public async Task GraphQLGraphType_NameofStringInterpolationMixAsKeyValue_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
                public string LastName { get; set; }
            }

            public class Product : ObjectGraphType
            {
                private const string FirstNameField = "FirstName";
                public Product()
                {
                    this.Key([$"{nameof(User.Id)} LastName", FirstNameField]);
                    this.Key([$"{nameof(User.Id)} {nameof(User.LastName)}", FirstNameField]);
                    Field<StringGraphType>("Id");
                    Field<StringGraphType>("FirstName");
                    Field<StringGraphType>("LastName");
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

        var key = graphType.FederationKeys[0];
        key.GetFieldNames().ShouldBe(["Id", "LastName", "FirstName"]);
        key.FieldsString.ShouldBe("Id LastName FirstName");

        key = graphType.FederationKeys[1];
        key.GetFieldNames().ShouldBe(["Id", "LastName", "FirstName"]);
        key.FieldsString.ShouldBe("Id LastName FirstName");
    }

    [Fact]
    public async Task GraphQLGraphType_ArrayWithNameof_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
                public string Name { get; set; }
            }

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key(new[] { nameof(User.Id), nameof(User.Name) });
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
        key.FieldsString.ShouldBe("Id Name");
        key.GetFieldNames().ShouldBe(["Id", "Name"]);
    }

    [Fact]
    public async Task GraphQLGraphType_CollectionExpressionWithNameof_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
                public string Name { get; set; }
            }

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key([nameof(User.Id), nameof(User.Name)]);
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
        key.FieldsString.ShouldBe("Id Name");
        key.GetFieldNames().ShouldBe(["Id", "Name"]);
    }

    [Fact]
    public async Task GraphQLGraphType_ArrayWithMixedNameofAndLiteral_ParsesCorrectly()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public string Id { get; set; }
            }

            public class Product : ObjectGraphType
            {
                public Product()
                {
                    this.Key(new[] { nameof(User.Id), "name" });
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
        key.FieldsString.ShouldBe("Id name");
        key.GetFieldNames().ShouldBe(["Id", "name"]);
    }
}
