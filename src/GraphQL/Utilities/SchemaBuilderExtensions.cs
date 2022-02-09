using GraphQL.Types;

namespace GraphQL.Utilities
{
    internal static class SchemaBuilderExtensions
    {
        private const string IS_EXTENSION_METAFIELD = "__IS_EXTENSION_METAFIELD__";

        public static bool? IsExtensionType(this IGraphType type)
        {
            return type.GetMetadata<bool?>(IS_EXTENSION_METAFIELD);
        }

        public static void SetIsExtensionType(this IGraphType type, bool isExtension)
        {
            type.Metadata[IS_EXTENSION_METAFIELD] = BoolBox.Boxed(isExtension);
        }
    }
}
