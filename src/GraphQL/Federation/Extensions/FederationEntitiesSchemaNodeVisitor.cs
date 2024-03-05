using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

internal class FederationEntitiesSchemaNodeVisitor : BaseSchemaNodeVisitor
{
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        var directives = type.GetAppliedDirectives();
        if (directives == null)
            return;
        if (type.GetAppliedDirectives()?.Any(d => d.Name == KEY_DIRECTIVE && !d.Any(arg => arg.Name == RESOLVABLE_ARGUMENT && (arg.Value is bool b && !b))) == true)
        {
            var entityType = schema.AllTypes["_Entity"] as UnionGraphType
                ?? throw new InvalidOperationException("The _Entity type is not defined in the schema.");
            entityType.AddPossibleType(type);
        }
    }
}
