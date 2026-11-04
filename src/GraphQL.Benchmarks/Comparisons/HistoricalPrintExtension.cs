using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Benchmarks.Historical;

// Copy of the internal AST Print helper used by both pinned rules. Keeping the
// same helper preserves argument-comparison behavior and allocations.
internal static class HistoricalPrintExtension
{
    private static readonly SDLPrinter _sdlPrinter = new();

    internal static string Print(this ASTNode node)
    {
        using var writer = new StringWriter();
        _sdlPrinter.PrintAsync(node, writer).GetAwaiter().GetResult();
        return writer.ToString()!;
    }
}
