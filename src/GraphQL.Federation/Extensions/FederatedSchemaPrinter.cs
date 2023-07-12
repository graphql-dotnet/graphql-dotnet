using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
using GraphQLParser.Visitors;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

internal class FederatedSchemaPrinter : SchemaPrinter
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
    { }


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
}
