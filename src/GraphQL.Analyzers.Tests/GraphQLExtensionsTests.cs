using GraphQL.Analyzers.Helpers;
using GraphQL.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Tests;

public class GraphQLExtensionsTests
{
    [Theory]
    [InlineData("GraphQL.Types", true)]
    [InlineData("Sample.Server", false)]
    public async Task IsGraphQLSymbol_TrueWhenDefinedInGraphQLLib(string @namespace, bool expected)
    {
        var tree = CSharpSyntaxTree.ParseText(
            $$"""
            namespace Sample.Server;

            public class MyClass
            {
            	{{@namespace}}.ISchema MyMethod() => null;
            }

            public interface ISchema { }
            """);

        var compilation = CreateCompilation(tree);
        var syntaxRoot = await tree.GetRootAsync();
        var myMethod = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var model = compilation.GetSemanticModel(tree);
        myMethod.ReturnType.IsGraphQLSymbol(model).ShouldBe(expected);
    }

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree) =>
        CSharpCompilation.Create(
            assemblyName: "GraphQL.Analyzers.Tests",
            syntaxTrees: [syntaxTree],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ISchema).Assembly.Location)
            ]);
}
