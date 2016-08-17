using System.IO;
using System.Linq;
using GraphQL.Language.AST;
using Xunit;

namespace GraphQL.Language.Tests
{
    public class ParserTests
    {
        [Fact]
        public void Parse_FieldInput_HasCorrectEndLocationAttribute()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(9, document.Location.End);
        }

        [Fact]
        public void Parse_FieldInput_HasCorrectStartLocationAttribute()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(0, document.Location.Start);
        }

        [Fact]
        public void Parse_FieldInput_HasOneOperationDefinition()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(ASTNodeKind.OperationDefinition, document.Definitions.First().Kind);
        }

        [Fact]
        public void Parse_FieldInput_NameIsNull()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Null(GetSingleOperationDefinition(document).Name);
        }

        [Fact]
        public void Parse_FieldInput_OperationIsQuery()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(OperationType.Query, GetSingleOperationDefinition(document).Operation);
        }

        [Fact]
        public void Parse_FieldInput_ReturnsDocumentNode()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(ASTNodeKind.Document, document.Kind);
        }

        [Fact]
        public void Parse_FieldInput_SelectionSetContainsSingleFieldSelection()
        {
            var document = ParseGraphQLFieldSource();

            Assert.Equal(ASTNodeKind.Field, GetSingleSelection(document).Kind);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_HasCorrectEndLocationAttribute()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(22, document.Location.End);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_HasCorrectStartLocationAttribute()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(0, document.Location.Start);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_HasOneOperationDefinition()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(ASTNodeKind.OperationDefinition, document.Definitions.First().Kind);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_NameIsNull()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal("Foo", GetSingleOperationDefinition(document).Name.Value);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_OperationIsQuery()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(OperationType.Mutation, GetSingleOperationDefinition(document).Operation);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_ReturnsDocumentNode()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(ASTNodeKind.Document, document.Kind);
        }

        [Fact]
        public void Parse_FieldWithOperationTypeAndNameInput_SelectionSetContainsSingleFieldWithOperationTypeAndNameSelection()
        {
            var document = ParseGraphQLFieldWithOperationTypeAndNameSource();

            Assert.Equal(ASTNodeKind.Field, GetSingleSelection(document).Kind);
        }

        [Fact]
        public void Parse_KitchenSink_DoesNotThrowError()
        {
            new Parser(new Lexer()).Parse(new Source(LoadKitchenSink()));
        }

        [Fact]
        public void Parse_NullInput_EmptyDocument()
        {
            var document = new Parser(new Lexer()).Parse(new Source(null));

            Assert.Equal(0, document.Definitions.Count());
        }

        [Fact]
        public void Parse_VariableInlineValues_DoesNotThrowError()
        {
            new Parser(new Lexer()).Parse(new Source("{ field(complex: { a: { b: [ $var ] } }) }"));
        }

        private static GraphQLOperationDefinition GetSingleOperationDefinition(GraphQLDocument document)
        {
            return ((GraphQLOperationDefinition)document.Definitions.Single());
        }

        private static ASTNode GetSingleSelection(GraphQLDocument document)
        {
            return GetSingleOperationDefinition(document).SelectionSet.Selections.Single();
        }

        private static string LoadKitchenSink()
        {
            string dataFilePath = Directory.GetCurrentDirectory() + "/data/KitchenSink.graphql";
            return File.ReadAllText(dataFilePath);
        }

        private static GraphQLDocument ParseGraphQLFieldSource()
        {
            return new Parser(new Lexer()).Parse(new Source("{ field }"));
        }

        private static GraphQLDocument ParseGraphQLFieldWithOperationTypeAndNameSource()
        {
            return new Parser(new Lexer()).Parse(new Source("mutation Foo { field }"));
        }
    }
}
