using GraphQL.Types;
using GraphQL.Analyzers.Tests.VerifiersExtensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GraphQL.Analyzers.Tests.SDK;

public sealed class TestContext
{
    private TestContext()
    {
    }

    public static async Task<TestContext> CreateAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = await CreateCompilationAsync(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = await syntaxTree.GetRootAsync();

        return new TestContext
        {
            SyntaxTree = syntaxTree,
            Compilation = compilation,
            SemanticModel = semanticModel,
            Root = root
        };
    }

    public SyntaxTree SyntaxTree { get; private set; }

    public CSharpCompilation Compilation { get; private set; }

    public SemanticModel SemanticModel { get; private set; }

    public SyntaxNode Root { get; private set; }

    private static async Task<CSharpCompilation> CreateCompilationAsync(SyntaxTree syntaxTree)
    {
        var references = await ReferenceResolver.ReferenceAssemblies
            .ResolveAsync(LanguageNames.CSharp, CancellationToken.None);

        return CSharpCompilation.Create(
            assemblyName: "GraphQL.Analyzers.Tests.SDK",
            syntaxTrees: [syntaxTree],
            references:
            [
                ..references,
                MetadataReference.CreateFromFile(typeof(ISchema).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MicrosoftDI.GraphQLBuilder).Assembly.Location)
            ]);
    }
}
