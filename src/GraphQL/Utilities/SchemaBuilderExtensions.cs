using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    internal static class SchemaBuilderExtensions
    {
        private const string __AST_MetaField__ = "__AST_MetaField__";

        public static T GetAstType<T>(this IProvideMetadata type) where T : class
        {
            if (type.Metadata.TryGetValue(__AST_MetaField__, out object value))
            {
                return value as T;
            }

            return null;
        }

        public static void SetAstType<T>(this IProvideMetadata type, T node) where T : ASTNode
        {
            type.Metadata[__AST_MetaField__] = node;
        }
    }
}
