using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Utilities.Visitors;

/// <summary>
/// Remove all Apollo Federation v1 types and fields from an AST.
/// Not necessary for Apollo Federation v2.
/// </summary>
public sealed class RemoveFederationTypesVisitor : ASTVisitor<RemoveFederationTypesVisitor.Context>
{
    private static readonly HashSet<string> _federatedDirectives =
    [
        EXTERNAL_DIRECTIVE,
        PROVIDES_DIRECTIVE,
        REQUIRES_DIRECTIVE,
        KEY_DIRECTIVE,
        "tag",
        "extends",
    ];

    private static readonly HashSet<string> _federatedTypes =
    [
        "_Entity",
        "_Any",
        "FieldSet",
        "_Service",
    ];

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
    protected override ValueTask VisitDocumentAsync(GraphQLDocument document, Context context)
    {
        // assume the query type name is Query
        context.QueryTypeName = "Query";

        // scan for the schema definition first, to get the query type name
        foreach (var definition in document.Definitions)
        {
            if (definition is GraphQLSchemaDefinition schemaDefinition)
            {
                foreach (var operationType in schemaDefinition.OperationTypes)
                {
                    if (operationType.Operation == OperationType.Query && operationType.Type != null) // operationType.Type should never be null
                    {
                        context.QueryTypeName = operationType.Type.Name;
                        break;
                    }
                }
                break;
            }
        }

        // remove all federation directives and federation types
        document.Definitions.RemoveAll(node => node switch
        {
            GraphQLDirectiveDefinition directive => _federatedDirectives.Contains(directive.Name.StringValue),
            GraphQLTypeDefinition type => _federatedTypes.Contains(type.Name.StringValue),
            _ => false,
        });

        return base.VisitDocumentAsync(document, context);
    }

    /// <inheritdoc/>
    protected override ValueTask VisitObjectTypeDefinitionAsync(GraphQLObjectTypeDefinition objectTypeDefinition, Context context)
    {
        if (objectTypeDefinition.Name.Value == context.QueryTypeName)
        {
            objectTypeDefinition.Fields?.Items.RemoveAll(
                field => field.Name.Value == "_service" || field.Name.Value == "_entities");
        }
        return base.VisitObjectTypeDefinitionAsync(objectTypeDefinition, context);
    }

    /// <inheritdoc/>
    protected override ValueTask VisitObjectTypeExtensionAsync(GraphQLObjectTypeExtension objectTypeExtension, Context context)
    {
        if (objectTypeExtension.Name.Value == context.QueryTypeName)
        {
            objectTypeExtension.Fields?.Items.RemoveAll(
                field => field.Name.Value == "_service" || field.Name.Value == "_entities");
        }
        return base.VisitObjectTypeExtensionAsync(objectTypeExtension, context);
    }

    /// <summary>
    /// Context for <see cref="RemoveFederationTypesVisitor"/>.
    /// </summary>
    public struct Context : IASTVisitorContext
    {
        /// <summary>
        /// The name of the query type.
        /// </summary>
        public ROM QueryTypeName { get; set; }

        /// <inheritdoc/>
        public CancellationToken CancellationToken => default;
    }
}
