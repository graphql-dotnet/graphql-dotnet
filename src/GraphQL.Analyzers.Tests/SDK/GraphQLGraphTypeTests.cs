using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLGraphTypeTests
{
    [Fact]
    public async Task TryCreate_NotGraphType_ReturnsNull()
    {
        var context = await TestContext.CreateAsync(
            """
            namespace Sample;

            public class MyClass
            {
                public MyClass()
                {
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldBeNull();
    }

    [Fact]
    public async Task TryCreate_ObjectGraphType_ReturnsInstance()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Name.ShouldBe("MyType");
        graphType.Syntax.ShouldBe(classDeclaration);
        graphType.SemanticModel.ShouldBe(context.SemanticModel);
    }

    [Fact]
    public async Task IsInputType_InputObjectGraphType_ReturnsTrue()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyInputType : InputObjectGraphType
            {
                public MyInputType()
                {
                    Field<StringGraphType>("name");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.IsInputType.ShouldBeTrue();
        graphType.IsOutputType.ShouldBeFalse();
    }

    [Fact]
    public async Task IsOutputType_ObjectGraphType_ReturnsTrue()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.IsOutputType.ShouldBeTrue();
        graphType.IsInputType.ShouldBeFalse();
    }

    [Fact]
    public async Task SourceType_GenericGraphType_ReturnsSourceType()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string Name { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.Name);
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.SourceType.ShouldNotBeNull();
        graphType.SourceType.Name.ShouldBe("Person");
    }

    [Fact]
    public async Task Fields_MultipleFields_ReturnsAllFields()
    {
        var context = await TestContext.CreateAsync(
            """
            using GraphQL.Types;

            namespace Sample;

            public class Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public int Age { get; set; }
            }

            public class PersonGraphType : ObjectGraphType<Person>
            {
                public PersonGraphType()
                {
                    Field(x => x.FirstName);
                    Field(x => x.LastName);
                    Field<IntGraphType>("age");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == "PersonGraphType");

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Fields.Count.ShouldBe(3);
        graphType.Fields[0].Name?.Value.ShouldBe("FirstName");
        graphType.Fields[1].Name?.Value.ShouldBe("LastName");
        graphType.Fields[2].Name?.Value.ShouldBe("age");
    }

    [Fact]
    public async Task GetField_ExistingField_ReturnsField()
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
                    Field<StringGraphType>("lastName");
                }
            }
            """);

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();

        var field = graphType.GetField("firstName");
        field.ShouldNotBeNull();
        field.Name?.Value.ShouldBe("firstName");
    }

    [Fact]
    public async Task GetField_NonExistingField_ReturnsNull()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();

        var field = graphType.GetField("nonExisting");
        field.ShouldBeNull();
    }

    [Fact]
    public async Task HasField_ExistingField_ReturnsTrue()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.HasField("firstName").ShouldBeTrue();
        graphType.HasField("nonExisting").ShouldBeFalse();
    }

    [Fact]
    public async Task TypeSymbol_ValidGraphType_ReturnsTypeSymbol()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.TypeSymbol.ShouldNotBeNull();
        graphType.TypeSymbol.Name.ShouldBe("MyType");
    }

    [Fact]
    public async Task Location_ValidGraphType_ReturnsClassLocation()
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

        var classDeclaration = context.Root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        graphType.ShouldNotBeNull();
        graphType.Location.ShouldNotBeNull();

        // Verify location points to class identifier
        var sourceText = await context.SyntaxTree.GetTextAsync();
        var locationText = sourceText.ToString(graphType.Location.SourceSpan);
        locationText.ShouldBe("MyType");
    }


}
