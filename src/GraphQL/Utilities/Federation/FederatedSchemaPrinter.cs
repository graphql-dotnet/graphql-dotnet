#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public class FederatedSchemaPrinter : SchemaPrinter //TODO:should be completely rewritten
    {
        private readonly List<string> _federatedDirectives = new List<string>
        {
            "external",
            "provides",
            "requires",
            "key",
            "extends"
        };

        private readonly List<string> _federatedTypes = new List<string>
        {
            "_Service",
            "_Entity",
            "_Any"
        };

        public FederatedSchemaPrinter(ISchema schema, SchemaPrinterOptions? options = null)
            : base(schema, options)
        {
        }

        public string PrintFederatedDirectives(IGraphType type)
        {
            Schema?.Initialize();

            return type.IsInputObjectType() ? "" : PrintFederatedDirectivesFromAst(type);
        }

        public string PrintFederatedDirectivesFromAst(IProvideMetadata type)
        {
            Schema?.Initialize();

            var astDirectives = type.GetAstType<IHasDirectivesNode>()?.Directives ?? type.GetExtensionDirectives<GraphQLDirective>();
            if (astDirectives == null)
                return "";

            var dirs = string.Join(
                " ",
                astDirectives
                    .Where(x => IsFederatedDirective((string)x.Name)) //TODO:alloc
                    .Select(PrintAstDirective)
            );

            return string.IsNullOrWhiteSpace(dirs) ? "" : $" {dirs}";
        }

        public string PrintAstDirective(GraphQLDirective directive)
        {
            Schema?.Initialize();

            return directive.Print();
        }

        public override string PrintObject(IObjectGraphType type)
        {
            Schema?.Initialize();

            var isExtension = type!.IsExtensionType();

            var interfaces = type!.ResolvedInterfaces.List.Select(x => x.Name).ToList();
            var delimiter = " & ";
            var implementedInterfaces = interfaces.Count > 0
                ? " implements {0}".ToFormat(string.Join(delimiter, interfaces))
                : "";

            var federatedDirectives = PrintFederatedDirectives(type);

            var extended = isExtension ? "extend " : "";

            if (type.Fields.Any(x => !IsFederatedType(x.ResolvedType!.GetNamedType().Name)))
                return FormatDescription(type.Description) + "{1}type {2}{3}{4} {{{0}{5}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, implementedInterfaces, federatedDirectives, PrintFields(type));
            else
                return FormatDescription(type.Description) + "{0}type {1}{2}{3}".ToFormat(extended, type.Name, implementedInterfaces, federatedDirectives);
        }

        public override string PrintInterface(IInterfaceGraphType type)
        {
            Schema?.Initialize();

            var isExtension = type.IsExtensionType();
            var extended = isExtension ? "extend " : "";

            return FormatDescription(type.Description) + "{1}interface {2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, PrintFields(type));
        }

        public override string PrintFields(IComplexGraphType type)
        {
            Schema?.Initialize();

            var fields = type?.Fields
                .Where(x => !IsFederatedType(x.ResolvedType!.GetNamedType().Name))
                .Select(x =>
                new
                {
                    x.Name,
                    Type = x.ResolvedType,
                    Args = PrintArgs(x),
                    Description = FormatDescription(x.Description, "  "),
                    Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : string.Empty,
                    FederatedDirectives = PrintFederatedDirectivesFromAst(x)
                }).ToList();

            return fields == null ? "" : string.Join(Environment.NewLine, fields.Select(
                f => "{3}  {0}{1}: {2}{4}{5}".ToFormat(f.Name, f.Args, f.Type, f.Description, f.Deprecation, f.FederatedDirectives)));
        }

        public string PrintFederatedSchema()
        {
            Schema?.Initialize();

            return PrintFilteredSchema(
                directiveName => !IsBuiltInDirective(directiveName) && !IsFederatedDirective(directiveName),
                typeName => !IsFederatedType(typeName) && IsDefinedType(typeName));
        }

        public bool IsFederatedDirective(string directiveName)
        {
            return _federatedDirectives.Contains(directiveName);
        }

        public bool IsFederatedType(string typeName)
        {
            return _federatedTypes.Contains(typeName);
        }
    }
}
