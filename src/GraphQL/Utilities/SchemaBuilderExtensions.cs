using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    internal static class SchemaBuilderExtensions
    {
        private const string AST_METAFIELD = "__AST_MetaField__";
        private const string EXTENSION_AST_METAFIELD = "__EXTENSION_AST_MetaField__";

        public static bool IsExtensionType(this IProvideMetadata type)
        {
            return type.HasExtensionAstTypes()
                && !type.AstTypeHasFields();
        }

        public static bool AstTypeHasFields(this IProvideMetadata type)
        {
            return GetAstType<ASTNode>(type) switch
            {
                GraphQLObjectTypeDefinition otd => otd.Fields.Any(),
                GraphQLInterfaceTypeDefinition itd => itd.Fields.Any(),
                _ => false
            };
        }

        public static T GetAstType<T>(this IProvideMetadata type) where T : class
        {
            return type.GetMetadata<T>(AST_METAFIELD);
        }

        public static TMetadataProvider SetAstType<TMetadataProvider>(this TMetadataProvider provider, ASTNode node)
            where TMetadataProvider : MetadataProvider
            => provider.WithMetadata(AST_METAFIELD, node);

        public static bool HasExtensionAstTypes(this IProvideMetadata type)
        {
            return GetExtensionAstTypes(type).Count > 0;
        }

        public static void AddExtensionAstType<T>(this MetadataProvider type, T astType) where T : ASTNode 
        {
            var types = GetExtensionAstTypes(type);
            types.Add(astType);
            type.Metadata[EXTENSION_AST_METAFIELD] = types;
        }

        public static List<ASTNode> GetExtensionAstTypes(this IProvideMetadata type)
        {
            return type.GetMetadata(EXTENSION_AST_METAFIELD, () => new List<ASTNode>());
        }

        public static IEnumerable<GraphQLDirective> GetExtensionDirectives<T>(this IProvideMetadata type) where T : ASTNode
        {
            var types = type.GetExtensionAstTypes().OfType<IHasDirectivesNode>().Where(n => n.Directives != null);
            return types.SelectMany(x => x.Directives);
        }
    }
}
