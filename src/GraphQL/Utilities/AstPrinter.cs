using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Utilities.Federation;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities
{
    /// <summary>
    /// Prints a string representation of the specified AST document or node.
    /// </summary>
    public static class AstPrinter // TODO: remove completely whole file
    {
        /// <summary>
        /// Returns a string representation of the specified node.
        /// </summary>
        public static string Print(ASTNode node)
        {
            var context = new PrintContext();
            _writer.Visit(node, context).GetAwaiter().GetResult(); // actually is sync
            //TODO: https://github.com/graphql-dotnet/parser/issues/155
            return context.Writer.ToString()!;
        }

        private static readonly SDLWriterEx _writer = new();

        private class SDLWriterEx : SDLWriter<PrintContext>
        {
            public override ValueTask Visit(ASTNode? node, PrintContext context)
            {
                return node switch
                {
                    AnyValue _ => throw new InvalidOperationException("TODO: may be implemented? see anynode_throws test"),
                    _ => base.Visit(node, context)
                };
            }
        }

        private class PrintContext : IWriteContext
        {
            public TextWriter Writer { get; set; } = new StringWriter();

            public Stack<ASTNode> Parents { get; set; } = new Stack<ASTNode>();

            public CancellationToken CancellationToken { get; set; }

            public int IndentLevel { get; set; }
        }
    }
}
