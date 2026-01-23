using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class FederationFieldsHelperTests
{
    [Theory]
    [InlineData("\"id\"", "id")]
    [InlineData("\"id name\"", "id name")]
    public async Task TryGetFieldsString_StringLiteral_ExtractsString(string expression, string expected)
    {
        var context = await TestContext.CreateAsync(
            $$"""
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                public TestType()
                {
                    this.Key({{expression}});
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe(expected);
    }

    [Fact]
    public async Task TryGetFieldsString_ConstField_ExtractsValue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                private const string RequiredFields = "id sku";

                public TestType()
                {
                    this.Key(RequiredFields);
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe("id sku");
    }

    [Fact]
    public async Task TryGetFieldsString_NameofExpression_ExtractsFieldName()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class User
            {
                public int Id { get; set; }
            }

            public class TestType : ObjectGraphType<User>
            {
                public TestType()
                {
                    this.Key(nameof(User.Id));
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe("Id");
    }

    [Fact]
    public async Task TryGetFieldsString_ImplicitArrayCreation_JoinsWithSpace()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                public TestType()
                {
                    this.Key(new[] { "id", "sku" });
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe("id sku");
    }

    [Fact]
    public async Task TryGetFieldsString_ExplicitArrayCreation_JoinsWithSpace()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                public TestType()
                {
                    this.Key(new string[] { "id", "sku" });
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe("id sku");
    }

    [Fact]
    public async Task TryGetFieldsString_InterpolatedString_ReturnsInterpolatedValue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                private const string Field1 = "id";
                private const string Field2 = "sku";

                public TestType()
                {
                    this.Key($"{Field1} {Field2}");
                }
            }
            """);

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe("id sku");
    }

    [Theory]
    [InlineData("\"id\"", "id")]
    [InlineData("\"id name\"", "id", "name")]
    [InlineData("[\"id\", \"name\"]", "id", "name")]
    [InlineData("new[] { \"id\", \"name\" }", "id", "name")]
    [InlineData("new string[] { \"id\", \"name\" }", "id", "name")]
    [InlineData("ConstFieldName", "Id")]
    [InlineData("Constants.ConstFieldName", "Id")]
    [InlineData("[ConstFieldName, \"name\"]", "Id", "name")]
    [InlineData("new[] { ConstFieldName, \"name\" }", "Id", "name")]
    [InlineData("new string[] { ConstFieldName, \"name\" }", "Id", "name")]
    [InlineData("nameof(User.Id)", "Id")]
    [InlineData("new[] { nameof(User.Id), \"name\" }", "Id", "name")]
    [InlineData("new string[] { nameof(User.Id), \"name\" }", "Id", "name")]
    [InlineData("$\"{nameof(User.Id)}\"", "Id")]
    [InlineData("$\"{nameof(User.Id)} name\"", "Id", "name")]
    [InlineData("$\"{ConstFieldName} name\"", "Id", "name")]
    [InlineData("$\"{ConstFieldName} name {nameof(User.Organization)}\"", "Id", "name", "Organization")]
    [InlineData("[$\"{ConstFieldName} organization\", \"name\"]", "Id", "organization", "name")]
    [InlineData("new[] { $\"{ConstFieldName} organization\", \"name\" }", "Id", "organization", "name")]
    [InlineData("new string[] { $\"{ConstFieldName} organization\", \"name\" }", "Id", "organization", "name")]
    [InlineData("[$\"{ConstFieldName} {nameof(User.Organization)}\", \"name\"]", "Id", "Organization", "name")]
    [InlineData("new[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }", "Id", "Organization", "name")]
    [InlineData("new string[] { $\"{ConstFieldName} {nameof(User.Organization)}\", \"name\" }", "Id", "Organization", "name")]
    public async Task TryGetFieldsString_VariousFormats_ExtractsCorrectly(string expression, params string[] expectedFields)
    {
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
                    this.Key({{expression}});

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

        var invocation = context.Root.FindMethodInvocation("Key");

        var argument = invocation.ArgumentList.Arguments[0].Expression;

        var result = FederationFieldsHelper.TryGetFieldsString(argument, context.SemanticModel, out var fieldsString);

        result.ShouldBeTrue();
        fieldsString.ShouldBe(string.Join(' ', expectedFields));
    }

    [Fact]
    public void ParseFields_ValidFieldsString_ReturnsSelectionSet()
    {
        var fieldsString = "id sku";

        var result = FederationFieldsHelper.ParseFields(fieldsString);

        result.ShouldNotBeNull();
        result.Selections.Count.ShouldBe(2);
    }

    [Fact]
    public void ParseFields_ComplexSelectionSet_ParsesNested()
    {
        var fieldsString = "price quantity discount { percentage }";

        var result = FederationFieldsHelper.ParseFields(fieldsString);

        result.ShouldNotBeNull();
        result.Selections.Count.ShouldBe(3);
    }

    [Fact]
    public void ParseFields_NullString_ReturnsNull()
    {
        var result = FederationFieldsHelper.ParseFields(null);

        result.ShouldBeNull();
    }

    [Fact]
    public void ParseFields_InvalidGraphQL_ReturnsNull()
    {
        var fieldsString = "id {";

        var result = FederationFieldsHelper.ParseFields(fieldsString);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetFieldNames_SimpleFields_ReturnsFieldNames()
    {
        var fieldsString = "id sku";
        var fields = FederationFieldsHelper.ParseFields(fieldsString);

        var fieldNames = FederationFieldsHelper.GetFieldNames(fields).ToList();

        fieldNames.Count.ShouldBe(2);
        fieldNames.ShouldContain("id");
        fieldNames.ShouldContain("sku");
    }

    [Fact]
    public void GetFieldNames_NestedFields_ReturnsTopLevelOnly()
    {
        var fieldsString = "price quantity discount { percentage }";
        var fields = FederationFieldsHelper.ParseFields(fieldsString);

        var fieldNames = FederationFieldsHelper.GetFieldNames(fields).ToList();

        fieldNames.Count.ShouldBe(3);
        fieldNames.ShouldContain("price");
        fieldNames.ShouldContain("quantity");
        fieldNames.ShouldContain("discount");
        fieldNames.ShouldNotContain("percentage");
    }

    [Fact]
    public void GetFieldNames_NullFields_ReturnsEmpty()
    {
        var fieldNames = FederationFieldsHelper.GetFieldNames(null).ToList();

        fieldNames.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFieldLocation_StringLiteral_ReturnsCorrectSpan()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                public TestType()
                {
                    this.Key("id sku");
                }
            }
            """);

        var invocation = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.ToString().Contains("Key"));

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        var sourceText = await context.SyntaxTree.GetTextAsync();

        var idLocation = FederationFieldsHelper.GetFieldLocation(
            argument,
            "id",
            -1,
            context.SyntaxTree,
            context.SemanticModel,
            invocation.GetLocation());

        var idText = sourceText.ToString(idLocation.SourceSpan);
        idText.ShouldBe("id");

        var skuLocation = FederationFieldsHelper.GetFieldLocation(
            argument,
            "sku",
            -1,
            context.SyntaxTree,
            context.SemanticModel,
            invocation.GetLocation());

        var skuText = sourceText.ToString(skuLocation.SourceSpan);
        skuText.ShouldBe("sku");
    }

    [Fact]
    public async Task GetFieldLocation_ArrayFormat_ReturnsCorrectSpan()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;
            using GraphQL.Federation;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
                public TestType()
                {
                    this.Key(new[] { "id", "sku" });
                }
            }
            """);

        var invocation = context.Root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.ToString().Contains("Key"));

        var argument = invocation.ArgumentList.Arguments[0].Expression;
        var sourceText = await context.SyntaxTree.GetTextAsync();

        var idLocation = FederationFieldsHelper.GetFieldLocation(
            argument,
            "id",
            -1,
            context.SyntaxTree,
            context.SemanticModel,
            invocation.GetLocation());

        var idText = sourceText.ToString(idLocation.SourceSpan);
        idText.ShouldBe("id");

        var skuLocation = FederationFieldsHelper.GetFieldLocation(
            argument,
            "sku",
            -1,
            context.SyntaxTree,
            context.SemanticModel,
            invocation.GetLocation());

        var skuText = sourceText.ToString(skuLocation.SourceSpan);
        skuText.ShouldBe("sku");
    }

    [Fact]
    public async Task GetFieldLocation_NullArgument_ReturnsFallbackLocation()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class TestType : ObjectGraphType
            {
            }
            """);

        var fallbackLocation = Microsoft.CodeAnalysis.Location.None;

        var result = FederationFieldsHelper.GetFieldLocation(
            null,
            "id",
            -1,
            context.SyntaxTree,
            context.SemanticModel,
            fallbackLocation);

        result.ShouldBe(fallbackLocation);
    }

    [Theory]
    [InlineData("id", "id", -1, 0)]
    [InlineData("id name", "id", -1, 0)]
    [InlineData("id name", "name", -1, 3)]
    [InlineData("id name id", "id", -1, 0)] // First occurrence
    [InlineData("id name id", "id", 8, 8)] // Second occurrence by position
    public void FindFieldInString_VariousScenarios_FindsCorrectPosition(
        string literalValue,
        string fieldName,
        int graphQLPosition,
        int expectedIndex)
    {
        var result = FederationFieldsHelper.FindFieldInString(literalValue, fieldName, graphQLPosition);

        result.ShouldBe(expectedIndex);
    }

    [Fact]
    public void FindFieldInString_NotFound_ReturnsNegative()
    {
        var result = FederationFieldsHelper.FindFieldInString("id name", "notFound", -1);

        result.ShouldBe(-1);
    }

    [Fact]
    public void FindFieldInString_WithBraces_SkipsStructuralCharacters()
    {
        var result = FederationFieldsHelper.FindFieldInString("user { id }", "id", -1);

        result.ShouldBe(7); // Position of 'id' after '{ '
    }
}
