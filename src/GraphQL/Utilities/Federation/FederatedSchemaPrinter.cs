using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Language;
using GraphQL.Types;
using GraphQLParser.AST;

namespace GraphQL.Utilities.Federation
{
    public class FederatedSchemaPrinter : SchemaPrinter
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

        private CoreToVanillaConverter _converter;

        public FederatedSchemaPrinter(ISchema schema, SchemaPrinterOptions options = null)
            : base(schema, options)
        {
            _converter = new CoreToVanillaConverter("");
        }

        public string PrintFederatedDirectives(IGraphType type)
        {
            if (type.IsInputObjectType()) return "";
            return PrintFederatedDirectivesFromAst(type);
        }

        public string PrintFederatedDirectivesFromAst(IProvideMetadata type)
        {
            var ast = type.GetAstType<IHasDirectivesNode>();
            if (ast?.Directives == null) return "";

            var dirs = string.Join(
                " ",
                ast.Directives
                    .Where(x => IsFederatedDirective(x.Name.Value))
                    .Select(PrintAstDirective)
            );

            return string.IsNullOrWhiteSpace(dirs) ? "" : $" {dirs}";
        }

        public string PrintAstDirective(GraphQLDirective directive)
        {
            var ast = _converter.Directive(directive);
            return AstPrinter.Print(ast);
        }

        public override string PrintObject(IObjectGraphType type)
        {
            var isExtension = type.IsExtensionType();

            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            var interfaces = type.ResolvedInterfaces.Select(x => x.Name).ToList();
            var delimiter = " & ";
            var implementedInterfaces = interfaces.Count > 0
                ? " implements {0}".ToFormat(string.Join(delimiter, interfaces))
                : "";

            var federatedDirectives = PrintFederatedDirectives(type);

            var extended = isExtension ? "extend " : "";

            return description + "{1}type {2}{3}{4} {{{0}{5}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, implementedInterfaces, federatedDirectives, PrintFields(type));
        }

        public override string PrintInterface(IInterfaceGraphType type)
        {
            var isExtension = type.IsExtensionType();
            var extended = isExtension ? "extend " : "";

            var description = Options.IncludeDescriptions ? PrintDescription(type.Description) : "";
            return description + "{1}interface {2} {{{0}{3}{0}}}".ToFormat(Environment.NewLine, extended, type.Name, PrintFields(type));
        }

        public override string PrintFields(IComplexGraphType type)
        {
            var fields = type?.Fields
                .Where(x => !IsFederatedType(x.ResolvedType.GetNamedType().Name))
                .Select(x =>
                new
                {
                    x.Name,
                    Type = ResolveName(x.ResolvedType),
                    Args = PrintArgs(x),
                    Description = Options.IncludeDescriptions ? PrintDescription(x.Description, "  ") : string.Empty,
                    Deprecation = Options.IncludeDeprecationReasons ? PrintDeprecation(x.DeprecationReason) : string.Empty,
                    FederatedDirectives = PrintFederatedDirectivesFromAst(x)
                }).ToList();

            return string.Join(Environment.NewLine, fields?.Select(
                f => "{3}  {0}{1}: {2}{4}{5}".ToFormat(f.Name, f.Args, f.Type, f.Description, f.Deprecation, f.FederatedDirectives)));
        }

        public string PrintFederatedSchema()
        {
            return PrintFilteredSchema(
                directiveName => !IsSpecDirective(directiveName) && !IsFederatedDirective(directiveName),
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
