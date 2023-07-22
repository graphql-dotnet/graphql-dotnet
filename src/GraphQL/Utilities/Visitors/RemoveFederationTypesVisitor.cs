using GraphQLParser.AST;
using GraphQLParser.Visitors;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Remove all Apollo Federation types and fields from an AST.
/// Not necessary for Apollo Federation v2.
/// </summary>
public sealed class RemoveFederationTypesVisitor : ASTVisitor<NullVisitorContext>
{
    private static readonly HashSet<string> _federatedDirectives = new()
    {
        "external",
        "provides",
        "requires",
        "key",
        "link",
        "shareable",
        "inaccessible",
        "tag",
        "override",
        "composeDirective",
        "interfaceObject",
        "extends",
    };

    private static readonly HashSet<string> _federatedTypes = new()
    {
        "_Entity",
        "_Any",
        "FieldSet",
        "link__Import",
        "link__Purpose",
        "_Service",
    };

    private static readonly RemoveFederationTypesVisitor _instance = new();

    private RemoveFederationTypesVisitor()
    {
    }

    /// <inheritdoc cref="RemoveFederationTypesVisitor"/>
    public static void Visit(ASTNode node)
    {
        _instance.VisitAsync(node, default).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    protected override ValueTask VisitDocumentAsync(GraphQLDocument document, NullVisitorContext context)
    {
        document.Definitions.RemoveAll(node => node switch
        {
            GraphQLDirectiveDefinition directive => _federatedDirectives.Contains(directive.Name.StringValue),
            GraphQLTypeDefinition type => _federatedTypes.Contains(type.Name.StringValue),
            _ => false,
        });
        return base.VisitDocumentAsync(document, context);
    }

    /// <inheritdoc/>
    protected override ValueTask VisitObjectTypeDefinitionAsync(GraphQLObjectTypeDefinition objectTypeDefinition, NullVisitorContext context)
    {
        if (objectTypeDefinition.Name.Value == "Query")
        {
            objectTypeDefinition.Fields?.Items.RemoveAll(
                field => field.Name.Value == "_service" || field.Name.Value == "_entities");
        }
        return base.VisitObjectTypeDefinitionAsync(objectTypeDefinition, context);
    }

    /// <inheritdoc/>
    protected override ValueTask VisitObjectTypeExtensionAsync(GraphQLObjectTypeExtension objectTypeExtension, NullVisitorContext context)
    {
        if (objectTypeExtension.Name.Value == "Query")
        {
            objectTypeExtension.Fields?.Items.RemoveAll(
                field => field.Name.Value == "_service" || field.Name.Value == "_entities");
        }
        return base.VisitObjectTypeExtensionAsync(objectTypeExtension, context);
    }
}
