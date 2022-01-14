using System.Linq;

namespace GraphQLParser.AST
{
    public static class GraphQLDocumentExtensions
    {
        public static GraphQLOperationDefinition Operation(this GraphQLDocument document)
        {
            return document.Definitions.OfType<GraphQLOperationDefinition>().First();
        }

        public static int OperationsCount(this GraphQLDocument document)
        {
            int count = 0;

            foreach (var def in document.Definitions)
            {
                if (def is GraphQLOperationDefinition)
                    ++count;
            }

            return count;
        }

        public static int FragmentsCount(this GraphQLDocument document)
        {
            int count = 0;

            foreach (var def in document.Definitions)
            {
                if (def is GraphQLFragmentDefinition)
                    ++count;
            }

            return count;
        }
    }
}
