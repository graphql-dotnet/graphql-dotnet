using GraphQL.Language;

namespace GraphQL.Execution
{
    public class SpracheDocumentBuilder : IDocumentBuilder
    {
        public Document Build(string data)
        {
            var input = new SourceInput(data);
            var result = GraphQLParser2.Document(input);

            var document = result.WasSuccessful ? result.Value : new Document();
            document.WasSuccessful = result.WasSuccessful;
            document.Expectations = result.Expectations;

            return document;
        }
    }
}
