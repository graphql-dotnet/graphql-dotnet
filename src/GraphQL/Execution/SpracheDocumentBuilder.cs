using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    public class SpracheDocumentBuilder : IDocumentBuilder
    {
        private static readonly Dictionary<string, Document> Cache = new Dictionary<string, Document>();

        public SpracheDocumentBuilder()
        {
            CacheDocuments = true;
        }

        public bool CacheDocuments { get; set; }

        public Document Build(string data)
        {
            Document document;

            if (Cache.TryGetValue(data, out document))
            {
                return document;
            }

            var input = new SourceInput(data);
            var result = GraphQLParser2.Document(input);

            document = result.WasSuccessful ? result.Value : new Document();
            document.WasSuccessful = result.WasSuccessful;
            document.Expectations = result.Expectations;

            if (document.WasSuccessful && CacheDocuments)
            {
                Cache[data] = document;
            }

            return document;
        }

        public void ResetCache()
        {
            Cache.Clear();
        }
    }
}
