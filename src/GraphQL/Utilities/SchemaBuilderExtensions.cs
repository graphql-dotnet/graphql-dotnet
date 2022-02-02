using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities
{
    internal static class SchemaBuilderExtensions
    {
        private const string AST_METAFIELD = "__AST_MetaField__"; // TODO: possible remove
        private const string EXTENSION_AST_METAFIELD = "__EXTENSION_AST_MetaField__"; // TODO: possible remove

        public static bool IsExtensionType(this IProvideMetadata type)
        {
            return type.HasExtensionAstTypes()
                && !type.AstTypeHasFields();
        }

        public static bool AstTypeHasFields(this IProvideMetadata type)
        {
            return GetAstType<ASTNode>(type) switch
            {
                GraphQLObjectTypeDefinition otd => otd.Fields?.Any() ?? false,
                GraphQLInterfaceTypeDefinition itd => itd.Fields?.Any() ?? false,
                _ => false
            };
        }

        public static T? GetAstType<T>(this IProvideMetadata type) where T : class // TODO: possible remove
        {
            return type.GetMetadata<T>(AST_METAFIELD);
        }

        public static TMetadataProvider SetAstType<TMetadataProvider>(this TMetadataProvider provider, ASTNode node) // TODO: possible remove
            where TMetadataProvider : IProvideMetadata

        {
            provider.WithMetadata(AST_METAFIELD, node); //TODO: remove?

            if (node is IHasDirectivesNode ast && ast.Directives?.Count > 0)
            {
                foreach (var directive in ast.Directives!)
                {
                    provider.ApplyDirective(directive!.Name.StringValue, d => //ISSUE:allocation
                    {
                        if (directive.Arguments?.Count > 0)
                        {
                            foreach (var arg in directive.Arguments)
                                d.AddArgument(new DirectiveArgument(arg.Name.StringValue) { Value = arg.Value.ParseAnyLiteral() }); //ISSUE:allocation
                        }
                    });
                }
            }

            return provider;
        }

        public static bool HasExtensionAstTypes(this IProvideMetadata type)
        {
            return GetExtensionAstTypes(type).Count > 0;
        }

        public static void AddExtensionAstType<T>(this IProvideMetadata type, T astType) where T : ASTNode
        {
            var types = GetExtensionAstTypes(type);
            types.Add(astType);
            type.Metadata[EXTENSION_AST_METAFIELD] = types;
        }

        public static List<ASTNode> GetExtensionAstTypes(this IProvideMetadata type)
        {
            return type.GetMetadata(EXTENSION_AST_METAFIELD, () => new List<ASTNode>())!;
        }

        public static IEnumerable<GraphQLDirective> GetExtensionDirectives<T>(this IProvideMetadata type) where T : ASTNode
        {
            var types = type.GetExtensionAstTypes().OfType<IHasDirectivesNode>().Where(n => n.Directives != null);
            return types.SelectMany(x => x.Directives!);
        }
    }
}
