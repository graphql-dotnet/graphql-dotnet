using System;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using GraphQL.Language.AST;
using GraphQL.Parsing;

namespace GraphQL.Execution
{
    [Obsolete("This document builder is now obsolete and will be removed in a future release.  Please use the GraphQLDocumentBuilder.")]
    public class AntlrDocumentBuilder : IDocumentBuilder
    {
        public Document Build(string body)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
            using (var reader = new StreamReader(stream))
            {
                var input = new AntlrInputStream(reader);
                var lexer = new GraphQLLexer(input);
                var tokens = new CommonTokenStream(lexer);
                var parser = new GraphQLParser(tokens);
                var documentTree = parser.document();
                var vistor = new GraphQLVisitor();
                return vistor.Visit(documentTree) as Document;
            }
        }
    }
}
