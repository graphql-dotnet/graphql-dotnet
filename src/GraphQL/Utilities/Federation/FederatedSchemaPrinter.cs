#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
using GraphQLParser.Visitors;
using static GraphQL.Utilities.Federation.FederationHelper;

namespace GraphQL.Utilities.Federation
{
    //todo: [Obsolete("Please use the schema.Print() extension method instead. This class will be removed in v9.")]
    public class FederatedSchemaPrinter : SchemaPrinter //TODO:should be completely rewritten
    {
        private static readonly HashSet<string> _federatedDirectives = new()
        {
            KEY_DIRECTIVE,
            SHAREABLE_DIRECTIVE,
            INACCESSIBLE_DIRECTIVE,
            OVERRIDE_DIRECTIVE,
            EXTERNAL_DIRECTIVE,
            PROVIDES_DIRECTIVE,
            REQUIRES_DIRECTIVE,
        };

        private static readonly HashSet<string> _federatedTypes = new()
        {
            "_Service",
            "_Entity",
            "_Any",
            "_Never",
        };
        private static readonly SDLPrinter _sdlPrinter = new();

        public FederatedSchemaPrinter(ISchema schema, SchemaPrinterOptions? options = null)
            : base(schema, options)
        {
        }

        public string PrintFederatedSchema()
        {
            var result = PrintFilteredSchema(
                directiveName => !IsBuiltInDirective(directiveName) && !IsFederatedDirective(directiveName),
                typeName => !IsFederatedType(typeName) && IsDefinedType(typeName));

            var linkSchemaExtension = Schema.GetMetadata<ASTNode>(LINK_SCHEMA_EXTENSION_METADATA);
            return $"{result}{Environment.NewLine}{Environment.NewLine}{PrintAstNode(linkSchemaExtension)}";
        }

        public override string PrintObject(IObjectGraphType type)
        {
            Schema?.Initialize();

            var interfaces = type.ResolvedInterfaces.Select(x => x.Name).ToList();
            var delimiter = Options.OldImplementsSyntax ? ", " : " & ";
            var implementedInterfaces = interfaces.Count > 0
                ? $" implements {string.Join(delimiter, interfaces)}"
                : "";

            var federatedDirectives = type.IsInputObjectType()
                ? string.Empty
                : PrintFederatedDirectives(type);

            if (type.Fields.Count > 0)
                return $"{FormatDescription(type.Description)}type {type.Name}{implementedInterfaces}{federatedDirectives} {{{Environment.NewLine}{PrintFields(type)}{Environment.NewLine}}}";
            else
            {
                return $"{FormatDescription(type.Description)}type {type.Name}{implementedInterfaces}{federatedDirectives}";
            }
        }

        public override string PrintFields(IComplexGraphType type)
        {
            Schema?.Initialize();

            var fields = type?.Fields
                .Where(x => !IsFederatedType(x.ResolvedType!.GetNamedType().Name))
                .Select(x => new
                {
                    x.Name,
                    Type = x.ResolvedType,
                    Args = PrintArgs(x),
                    Description = FormatDescription(x.Description, "  "),
                    Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : string.Empty,
                    FederatedDirectives = PrintFederatedDirectives(x)
                }).ToList();

            return fields == null
                ? string.Empty
                : string.Join(Environment.NewLine, fields.Select(f => $"{f.Description}  {f.Name}{f.Args}: {f.Type}{f.Deprecation}{f.FederatedDirectives}"));
        }


        private string PrintFederatedDirectives(IProvideMetadata type)
        {
            var directives = type.GetMetadata<IHasDirectivesNode>(AST_METAFIELD)?.Directives;
            if (directives == null)
                return string.Empty;
            var result = string.Join(" ", directives
                .Where(x => IsFederatedDirective(x.Name.StringValue))
                .Select(PrintAstNode));
            return string.IsNullOrWhiteSpace(result) ? string.Empty : $" {result}";
        }

        private static string PrintAstNode(ASTNode node)
        {
            using var writer = new StringWriter();
            _sdlPrinter.PrintAsync(node, writer).AsTask().GetAwaiter().GetResult();
            return writer.ToString();
        }

        private bool IsFederatedDirective(string directiveName) => _federatedDirectives.Contains(directiveName);

        private bool IsFederatedType(string typeName) => _federatedTypes.Contains(typeName);

        // public string PrintFederatedDirectives(IGraphType type)
        // {
        //     Schema?.Initialize();

        //     return type.IsInputObjectType() ? "" : PrintFederatedDirectivesFromAst(type);
        // }

        // public string PrintFederatedDirectivesFromAst(IProvideMetadata type)
        // {
        //     Schema?.Initialize();

        //     var astDirectives = type.GetAstType<IHasDirectivesNode>()?.Directives ?? type.GetExtensionDirectives<GraphQLDirective>();
        //     if (astDirectives == null)
        //         return "";

        //     var dirs = string.Join(
        //         " ",
        //         astDirectives
        //             .Where(x => IsFederatedDirective((string)x.Name)) //TODO:alloc
        //             .Select(PrintAstDirective)
        //     );

        //     return string.IsNullOrWhiteSpace(dirs) ? "" : $" {dirs}";
        // }

        // public string PrintAstDirective(GraphQLDirective directive)
        // {
        //     Schema?.Initialize();

        //     return directive.Print();
        // }

        // public override string PrintObject(IObjectGraphType type)
        // {
        //     Schema?.Initialize();

        //     var isExtension = type!.IsExtensionType();

        //     var interfaces = type!.ResolvedInterfaces.List.Select(x => x.Name).ToList();
        //     var delimiter = " & ";
        //     var implementedInterfaces = interfaces.Count > 0
        //         ? " implements {0}".ToFormat(string.Join(delimiter, interfaces))
        //         : "";

        //     var federatedDirectives = PrintFederatedDirectives(type);

        //     var extended = isExtension ? "extend " : "";

        //     if (type.Fields.Any(x => !IsFederatedType(x.ResolvedType!.GetNamedType().Name)))
        //         return FormatDescription(type.Description) + "{1}type {2}{3}{4} {{{0}{5}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, implementedInterfaces, federatedDirectives, PrintFields(type));
        //     else
        //         return FormatDescription(type.Description) + "{0}type {1}{2}{3}".ToFormat(extended, type.Name, implementedInterfaces, federatedDirectives);
        // }

        // public override string PrintInterface(IInterfaceGraphType type)
        // {
        //     Schema?.Initialize();

        //     var isExtension = type.IsExtensionType();
        //     var extended = isExtension ? "extend " : "";

        //     return FormatDescription(type.Description) + "{1}interface {2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, PrintFields(type));
        // }

        // public override string PrintFields(IComplexGraphType type)
        // {
        //     Schema?.Initialize();

        //     var fields = type?.Fields
        //         .Where(x => !IsFederatedType(x.ResolvedType!.GetNamedType().Name))
        //         .Select(x =>
        //         new
        //         {
        //             x.Name,
        //             Type = x.ResolvedType,
        //             Args = PrintArgs(x),
        //             Description = FormatDescription(x.Description, "  "),
        //             Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : string.Empty,
        //             FederatedDirectives = PrintFederatedDirectivesFromAst(x)
        //         }).ToList();

        //     return fields == null ? "" : string.Join(Environment.NewLine, fields.Select(
        //         f => "{3}  {0}{1}: {2}{4}{5}".ToFormat(f.Name, f.Args, f.Type, f.Description, f.Deprecation, f.FederatedDirectives)));
        // }

        // public string PrintFederatedSchema()
        // {
        //     Schema?.Initialize();

        //     return PrintFilteredSchema(
        //         directiveName => !IsBuiltInDirective(directiveName) && !IsFederatedDirective(directiveName),
        //         typeName => !IsFederatedType(typeName) && IsDefinedType(typeName));
        // }

        // public bool IsFederatedDirective(string directiveName)
        // {
        //     return _federatedDirectives.Contains(directiveName);
        // }

        // public bool IsFederatedType(string typeName)
        // {
        //     return _federatedTypes.Contains(typeName);
        // }
    }
}
