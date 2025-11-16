using GraphQL.Analyzers.SDK;
using GraphQL.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests.SDK;

public class GraphQLSyntaxExtensionsTests
{
    [Fact]
    public async Task AsGraphQLField_FieldInvocation_ReturnsFieldInvocation()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.ToString().Contains("Field"));

        var field = invocation.AsGraphQLField(model);

        field.ShouldNotBeNull();
        field.Name?.Value.ShouldBe("name");
    }

    [Fact]
    public async Task AsGraphQLField_NotFieldInvocation_ReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        var field = invocation.AsGraphQLField(model);

        field.ShouldBeNull();
    }

    [Fact]
    public async Task AsGraphQLGraphType_GraphTypeClass_ReturnsGraphType()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = classDeclaration.AsGraphQLGraphType(model);

        graphType.ShouldNotBeNull();
        graphType.Name.ShouldBe("MyType");
    }

    [Fact]
    public async Task AsGraphQLGraphType_NotGraphTypeClass_ReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            namespace Sample;

            public class MyClass
            {
            }
            """);

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        var graphType = classDeclaration.AsGraphQLGraphType(model);

        graphType.ShouldBeNull();
    }

    [Fact]
    public async Task AsGraphQLFieldArgument_ArgumentInvocation_ReturnsArgument()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.ToString().Contains("Argument"));

        var argument = invocation.AsGraphQLFieldArgument(model);

        argument.ShouldNotBeNull();
        argument.Name?.Value.ShouldBe("limit");
    }

    [Fact]
    public async Task GetGraphQLFields_MultipleFields_ReturnsAllFields()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                    Field<StringGraphType>("firstName");
                    Field<StringGraphType>("lastName");
                    Field<IntGraphType>("age");
                }
            }
            """);

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var fields = root.GetGraphQLFields(model).ToList();

        fields.Count.ShouldBe(3);
        fields[0].Name?.Value.ShouldBe("firstName");
        fields[1].Name?.Value.ShouldBe("lastName");
        fields[2].Name?.Value.ShouldBe("age");
    }

    [Fact]
    public async Task GetGraphQLFields_NoFields_ReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            using GraphQL.Types;

            namespace Sample;

            public class MyType : ObjectGraphType
            {
                public MyType()
                {
                }
            }
            """);

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var fields = root.GetGraphQLFields(model).ToList();

        fields.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetGraphQLGraphTypes_MultipleGraphTypes_ReturnsAllGraphTypes()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            using GraphQL.Types;

            namespace Sample;

            public class PersonGraphType : ObjectGraphType
            {
                public PersonGraphType()
                {
                    Field<StringGraphType>("name");
                }
            }

            public class AddressGraphType : ObjectGraphType
            {
                public AddressGraphType()
                {
                    Field<StringGraphType>("street");
                }
            }
            """);

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var graphTypes = root.GetGraphQLGraphTypes(model).ToList();

        graphTypes.Count.ShouldBe(2);
        graphTypes[0].Name.ShouldBe("PersonGraphType");
        graphTypes[1].Name.ShouldBe("AddressGraphType");
    }

    [Fact]
    public async Task GetGraphQLGraphTypes_NoGraphTypes_ReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText(
            """
            namespace Sample;

            public class MyClass
            {
            }

            public class AnotherClass
            {
            }
            """);

        var compilation = CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var root = await tree.GetRootAsync();

        var graphTypes = root.GetGraphQLGraphTypes(model).ToList();

        graphTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task FirstAncestorOrSelf_FindsMatchingAncestor()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var root = await tree.GetRootAsync();

        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        var classDecl = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();

        classDecl.ShouldNotBeNull();
        classDecl.Identifier.Text.ShouldBe("MyType");
    }

    [Fact]
    public async Task FirstAncestorOrSelf_NoMatchingAncestor_ReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText(
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

        var root = await tree.GetRootAsync();

        var invocation = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        // InterfaceDeclarationSyntax doesn't exist in the tree
        var interfaceDecl = invocation.FirstAncestorOrSelf<InterfaceDeclarationSyntax>();

        interfaceDecl.ShouldBeNull();
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        // Get basic references
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISchema).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ObjectGraphType).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        // Add reference to System.Runtime for record types and init properties
        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        return CSharpCompilation.Create(
            assemblyName: "GraphQL.Analyzers.Tests.SDK",
            syntaxTrees: [syntaxTree],
            references: references);
    }
}
