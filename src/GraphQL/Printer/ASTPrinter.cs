using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities
{
    internal class ASTPrinter : SDLPrinter
    {
        private static readonly List<ROM> _builtInScalars = new()
        {
            "String",
            "Boolean",
            "Int",
            "Float",
            "ID"
        };

        private static readonly List<ROM> _builtInDirectives = new()
        {
            "skip",
            "include",
            "deprecated"
        };

        public ASTPrinter(SchemaPrinterOptions2 options)
            : base(options)
        {
        }

        protected static bool IsIntrospectionType(ROM typeName) => typeName.Length >= 2 && typeName.Span[0] == '_' && typeName.Span[1] == '_';

        protected static bool IsBuiltInScalar(ROM typeName) => _builtInScalars.Contains(typeName);

        protected static bool IsBuiltInDirective(ROM directiveName) => _builtInDirectives.Contains(directiveName);

        protected override ValueTask VisitDescriptionAsync(GraphQLDescription description, DefaultPrintContext context)
        {
            return ((SchemaPrinterOptions2)Options).IncludeDescriptions
                ? base.VisitDescriptionAsync(description, context)
                : default;
        }

        protected override ValueTask VisitDirectiveDefinitionAsync(GraphQLDirectiveDefinition directiveDefinition, DefaultPrintContext context)
        {
            return ((SchemaPrinterOptions2)Options).IncludeBuiltinDirectives || !IsBuiltInDirective(directiveDefinition.Name)
                ? base.VisitDirectiveDefinitionAsync(directiveDefinition, context)
                : default;
        }

        protected override ValueTask VisitScalarTypeDefinitionAsync(GraphQLScalarTypeDefinition scalarTypeDefinition, DefaultPrintContext context)
        {
            return ((SchemaPrinterOptions2)Options).IncludeBuiltinScalars || !IsBuiltInScalar(scalarTypeDefinition.Name)
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
                ? default
                : base.VisitAsync(node, context);
        }
    }
}
