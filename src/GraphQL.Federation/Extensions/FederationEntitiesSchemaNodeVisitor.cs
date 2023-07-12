using GraphQL.Federation.Types;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQLParser.AST;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

internal class FederationEntitiesSchemaNodeVisitor : BaseSchemaNodeVisitor
{
    private readonly EntityType _entityType;


    public FederationEntitiesSchemaNodeVisitor(EntityType entityType)
    {
        _entityType = entityType;
    }


    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        var astMetafield = type.GetMetadata<IHasDirectivesNode>(AST_METAFIELD);
        if (astMetafield?.Directives?.Items?.Any(x =>
            x.Name == KEY_DIRECTIVE
            && !x.Arguments!.Any(y => y.Name == RESOLVABLE_ARGUMENT && y.Value is GraphQLFalseBooleanValue)) == true)
        {
            _entityType.AddPossibleType(type);
        }
    }
}
