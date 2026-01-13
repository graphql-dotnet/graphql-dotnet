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
    public async Task GraphQLGraphType_SingleKeyDirectives_ReturnsAllKeys()
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
    [InlineData(10, "\"id\"", "id")]
    [InlineData(11, "\"id name\"", "id", "name")]
    [InlineData(12, "[\"id\", \"name\"]", "id", "name")]
    [InlineData(13, "new[] { \"id\", \"name\" }", "id", "name")]
    [InlineData(14, "new string[] { \"id\", \"name\" }", "id", "name")]
    // const
    [InlineData(15, "ConstFieldName", "Id")]
    [InlineData(16, "Constants.ConstFieldName", "Id")]
    [InlineData(17, "[ConstFieldName, \"name\"]", "Id", "name")]
    [InlineData(18, "new[] { ConstFieldName, \"name\" }", "Id", "name")]
    [InlineData(19, "new string[] { ConstFieldName, \"name\" }", "Id", "name")]
    // nameof
    [InlineData(20, "nameof(User.Id)", "Id")]
    [InlineData(21, "new[] { nameof(User.Id), \"name\" }", "Id", "name")]
    [InlineData(22, "new string[] { nameof(User.Id), \"name\" }", "Id", "name")]
    // interpolation
    [InlineData(23, "$\"{nameof(User.Id)}\"", "Id")]
    [InlineData(24, "$\"{nameof(User.Id)} name\"", "Id", "name")]
    [InlineData(25, "$\"{ConstFieldName} name\"", "Id", "name")]
    [InlineData(26, "$\"{ConstFieldName} name {nameof(User.Organization)}\"", "Id", "name", "Organization")]
    [InlineData(27, "[$\"{ConstFieldName} organization\", \"name\"]", "Id", "organization", "name")]
    [InlineData(28, "new[] { $\"{ConstFieldName} organization\", \"name\" }", "Id", "organization", "name")]
    [InlineData(29, "new string[] { $\"{ConstFieldName} organization\", \"name\" }", "Id", "organization", "name")]
    [InlineData(30, "[$\"{ConstFieldName} {nameof(User.Organization)}\", \"name\"]", "Id", "Organization", "name")]
    [InlineData(31, "new[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }", "Id", "Organization", "name")]
    [InlineData(32, "new string[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }", "Id", "Organization", "name")]
    public async Task FederationKey_PlainFields_ParsesCorrectly(int idx, string keyExpression, params string[] expectedFields)
    {
        _ = idx;

        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Federation;
            using GraphQL.Types;

            namespace Sample.Server;

            public class UserGraphType : ObjectGraphType<User>
            {
                private const string ConstFieldName = "Id";

                public UserGraphType()
                {
                    this.Key({{keyExpression}});

                    Field(x => x.Id, type: typeof(NonNullGraphType<IdGraphType>));
                    Field<NonNullGraphType<StringGraphType>>("name");
                    Field<NonNullGraphType<StringGraphType>>("organization");
                }
            }

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Organization { get; set; }
            }

            public class Constants
            {
                public const string ConstFieldName = "Id";
            }
            """);

        var classDeclaration = context.Root.FindClassDeclaration("UserGraphType");
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.FederationKeys.ShouldNotBeNull();
        graphType.FederationKeys.Count.ShouldBe(1);

        var key = graphType.FederationKeys[0];
        key.FieldsString.ShouldBe(string.Join(' ', expectedFields));
        key.GetFieldNames().ShouldBe(expectedFields);
        key.Fields.ShouldNotBeNull();
        key.Fields.Selections.Count.ShouldBe(expectedFields.Length);
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

        var classDeclaration = context.Root.FindClassDeclaration("Product");
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
