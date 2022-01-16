namespace GraphQLParser.AST
{
    /// <summary>
    /// Extension methods for <see cref="GraphQLDocument"/>.
    /// </summary>
    public static class GraphQLDocumentExtensions
    {
        /// <summary>
        /// Gets count of operations in the specified document.
        /// </summary>
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

        /// <summary>
        /// Gets count of fragments in the specified document.
        /// </summary>
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
