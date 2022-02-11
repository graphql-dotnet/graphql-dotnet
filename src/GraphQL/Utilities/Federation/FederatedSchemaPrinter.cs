using GraphQL.Types;

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

            return type.IsInputObjectType() ? "" : PrintFederatedDirectives(type);
        }

        public string PrintFederatedDirectives(IProvideMetadata type)
        {
            Schema?.Initialize();

            var directives = type.GetAppliedDirectives();
            if (directives == null)
                return "";

            var dirs = string.Join(
                " ",
                directives
                    .Where(x => IsFederatedDirective(x.Name))
                    .Select(PrintAstDirective)
            );

            return string.IsNullOrWhiteSpace(dirs) ? "" : $" {dirs}";
        }

        public string PrintAstDirective(AppliedDirective directive)
        {
            Schema?.Initialize();

            var astDirective = new ASTConverter().ConvertDirective(directive, Schema);
            return astDirective.Print();
        }

        public override string PrintObject(IObjectGraphType type)
        {
            Schema?.Initialize();

            var isExtension = type!.IsExtensionType() == true;

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

            var isExtension = type.IsExtensionType() == true;
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
                    FederatedDirectives = PrintFederatedDirectives(x)
                }).ToList();

            return string.Join(Environment.NewLine, fields?.Select(
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
