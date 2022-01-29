using System.Threading.Tasks;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities
{
    internal class ASTIntrospectionPrinter : SDLPrinter
    {
        public ASTIntrospectionPrinter(SchemaIntrospectionPrinterOptions options)
         : base(options)
        {

        }

        protected static bool IsIntrospectionType(ROM typeName) => typeName.Length >= 2 && typeName.Span[0] == '_' && typeName.Span[1] == '_';

        protected override ValueTask VisitDescriptionAsync(GraphQLDescription description, DefaultPrintContext context)
        {
            return ((SchemaIntrospectionPrinterOptions)Options).IncludeDescriptions
                ? base.VisitDescriptionAsync(description, context)
                : default;
        }

        protected override ValueTask VisitDirectiveDefinitionAsync(GraphQLDirectiveDefinition directiveDefinition, DefaultPrintContext context)
        {
            return ((SchemaIntrospectionPrinterOptions)Options).IncludeBuiltinDirectives
                ? base.VisitDirectiveDefinitionAsync(directiveDefinition, context)
                : default;
        }

        protected override ValueTask VisitScalarTypeDefinitionAsync(GraphQLScalarTypeDefinition scalarTypeDefinition, DefaultPrintContext context)
        {
            return ((SchemaIntrospectionPrinterOptions)Options).IncludeBuiltinScalars
                ? base.VisitScalarTypeDefinitionAsync(scalarTypeDefinition, context)
                : default;
        }

        protected override ValueTask VisitDirectiveAsync(GraphQLDirective directive, DefaultPrintContext context)
        {
            return ((SchemaPrinterOptions2)Options).IncludeDeprecationReasons || directive.Name != "deprecated"
                ? base.VisitDirectiveAsync(directive, context)
                : default;
        }

        public override ValueTask VisitAsync(ASTNode? node, DefaultPrintContext context)
        {
            return node is GraphQLTypeDefinition typeDef && IsIntrospectionType(typeDef.Name)
                ? base.VisitAsync(node, context)
                : default;
        }
    }
}
