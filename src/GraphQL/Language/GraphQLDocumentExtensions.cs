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

        /// <summary>
        /// Returns the first matching field node contained within this object value
        /// node that matches the specified name, or <see langword="null"/> otherwise.
        /// </summary>
        public static GraphQLObjectField? Field(this GraphQLObjectValue objectValue, ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            if (objectValue.Fields != null)
            {
                foreach (var field in objectValue.Fields)
                {
                    if (field.Name.Value == name)
                        return field;
                }
            }

            return null;
        }
    }
}
