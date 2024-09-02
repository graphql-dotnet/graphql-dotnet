using GraphQL.Validation.Errors;
using GraphQLParser.AST;

namespace GraphQL.Validation.Rules;

/// <summary>
/// Known directives:
///
/// GraphQL servers define what directives they support and where they support them.
/// For each usage of a directive, the directive must be available on that server and
/// must be used in a location that the server has declared support for.
/// </summary>
public class KnownDirectivesInAllowedLocations : ValidationRuleBase
{
    /// <summary>
    /// Returns a static instance of this validation rule.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly KnownDirectivesInAllowedLocations Instance = new();
#pragma warning restore CS0618 // Type or member is obsolete

    /// <inheritdoc cref="KnownDirectivesInAllowedLocations"/>
    [Obsolete("Please use the Instance property to retrieve a static instance. This constructor will be removed in v9.")]
    public KnownDirectivesInAllowedLocations()
    {
    }

    /// <inheritdoc/>
    /// <exception cref="KnownDirectivesError"/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_nodeVisitor);

    private static readonly INodeVisitor _nodeVisitor = new MatchingNodeVisitor<GraphQLDirective>((node, context) =>
    {
        var directiveDef = context.Schema.Directives.Find(node.Name);
        if (directiveDef == null)
        {
            context.ReportError(new KnownDirectivesError(context, node));
        }
        else
        {
            var candidateLocation = DirectiveLocationForAstPath(context);
            if (!directiveDef.Locations.Contains(candidateLocation))
            {
                context.ReportError(new DirectivesInAllowedLocationsError(context, node, candidateLocation));
            }
        }
    });

    // From https://spec.graphql.org/October2021/#sec-Document:
    // Documents are only executable by a GraphQL service if they are ExecutableDocument and contain at least
    // one OperationDefinition. A Document which contains TypeSystemDefinitionOrExtension must not be executed;
    // GraphQL execution services which receive a Document containing these should return a descriptive error.
    //
    // Nevertheless, this method calculates all possible locations.
    private static DirectiveLocation DirectiveLocationForAstPath(ValidationContext context)
    {
        var appliedTo = context.TypeInfo.GetAncestor(1);

        if (appliedTo is GraphQLDirectives || appliedTo is GraphQLArguments)
        {
            appliedTo = context.TypeInfo.GetAncestor(2);
        }

        return appliedTo switch
        {
            // https://spec.graphql.org/October2021/#ExecutableDirectiveLocation
            GraphQLOperationDefinition op => op.Operation switch
            {
                OperationType.Query => DirectiveLocation.Query,
                OperationType.Mutation => DirectiveLocation.Mutation,
                OperationType.Subscription => DirectiveLocation.Subscription,
                _ => throw new InvalidOperationException($"Unknown operation type '{op.Operation}.")
            },
            GraphQLField _ => DirectiveLocation.Field,
            GraphQLFragmentDefinition _ => DirectiveLocation.FragmentDefinition,
            GraphQLFragmentSpread _ => DirectiveLocation.FragmentSpread,
            GraphQLInlineFragment _ => DirectiveLocation.InlineFragment,
            GraphQLVariableDefinition _ => DirectiveLocation.VariableDefinition,

            // https://spec.graphql.org/October2021/#TypeSystemDirectiveLocation
            GraphQLSchemaDefinition _ => DirectiveLocation.Schema,
            GraphQLScalarTypeDefinition _ => DirectiveLocation.Scalar,
            GraphQLObjectTypeDefinition _ => DirectiveLocation.Object,
            GraphQLFieldDefinition _ => DirectiveLocation.FieldDefinition,
            //GraphQLArgument?s?Definition => DirectiveLocation.ArgumentDefinition, //TODO: ???
            GraphQLInterfaceTypeDefinition _ => DirectiveLocation.Interface,
            GraphQLUnionTypeDefinition _ => DirectiveLocation.Union,
            GraphQLEnumTypeDefinition _ => DirectiveLocation.Enum,
            GraphQLEnumValueDefinition _ => DirectiveLocation.EnumValue, //TODO: https://github.com/graphql/graphql-spec/issues/924
            GraphQLEnumValue _ => DirectiveLocation.EnumValue,
            GraphQLInputObjectTypeDefinition _ => DirectiveLocation.InputObject,
            GraphQLInputFieldsDefinition _ => DirectiveLocation.InputFieldDefinition,
            _ => throw new InvalidOperationException($"Unable to determine directive location for '{appliedTo?.Print()}'.")
        };
    }
}
