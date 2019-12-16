using System.Collections.Generic;
using System.Linq;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    internal static class SchemaBuilderExtensions
    {
        private const string __AST_MetaField__ = "__AST_MetaField__";
        private const string __EXTENSION_AST_MetaField__ = "__EXTENSION_AST_MetaField__";

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
            return type.GetMetadata<T>(__AST_MetaField__);
        }

        public static void SetAstType<T>(this IProvideMetadata type, T node) where T : ASTNode
        {
            type.Metadata[__AST_MetaField__] = node;
        }

        public static bool HasExtensionAstTypes(this IProvideMetadata type)
        {
            return GetExtensionAstTypes(type).Count > 0;
        }

        public static void AddExtensionAstType<T>(this IProvideMetadata type, T astType) where T : ASTNode 
        {
            var types = GetExtensionAstTypes(type);
            types.Add(astType);
            type.Metadata[__EXTENSION_AST_MetaField__] = types;
        }

        public static List<ASTNode> GetExtensionAstTypes(this IProvideMetadata type)
        {
            return type.GetMetadata(__EXTENSION_AST_MetaField__, () => new List<ASTNode>());
        }

        public static IEnumerable<GraphQLDirective> GetExtensionDirectives<T>(this IProvideMetadata type) where T : ASTNode
        {
            var types = type.GetExtensionAstTypes().OfType<IHasDirectivesNode>();
            return types.SelectMany(x => x.Directives);
        }
    }
}
