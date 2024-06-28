using System.Collections;
using GraphQL.DI;
using GraphQL.Federation.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation.Visitors;

/// <summary>
/// This adds the <c>_entities</c> field to the query type.
/// This cannot be added via an <see cref="IConfigureSchema"/> instance because the
/// <see cref="ISchema.Query"/> property will not yet be set. Requires the
/// <c>_Entity</c> and <c>_Any</c> types to have been already defined in the schema.
/// This visitor also adds types to the <c>_Entity</c> union type.
/// <para>
/// If no entities are resolvable, the <c>_Entity</c> type is removed from the schema
/// and the <c>_entities</c> field is not added to the query type.
/// </para>
/// </summary>
/// <remarks>
/// Since the schema node visitors run after the schema has been initialized, we must
/// set the ResolvedType property of the fields to type pulled from the schema, rather
/// than setting the Type property directly. It is also not possible to use the
/// <see cref="GraphQLTypeReference"/> meta type to reference the types, as it is
/// normally resolved during schema initialization, which has already occurred.
/// </remarks>
internal class FederationEntitiesSchemaNodeVisitor : BaseSchemaNodeVisitor
{
    private Func<AppliedDirective, bool> _keyDirectivePredicate = d => d.Name == KEY_DIRECTIVE;
    public override void VisitSchema(ISchema schema)
    {
        var linkedSchemas = schema.GetLinkedSchemas();
        var link = linkedSchemas?.Where(x => x.Url.StartsWith(FEDERATION_LINK_PREFIX)).FirstOrDefault();
        if (link != null)
        {
            var keyDirectiveName = link.NameForDirective(KEY_DIRECTIVE);
            if (keyDirectiveName == KEY_DIRECTIVE)
                return;
            _keyDirectivePredicate = d => d.Name == keyDirectiveName || (d.Name == KEY_DIRECTIVE && (d.FromSchemaUrl == FEDERATION_LINK_SCHEMA_URL || d.FromSchemaUrl == link.Url));
        }
    }

    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        var directives = type.GetAppliedDirectives();
        if (directives == null)
            return;
        if (type.GetAppliedDirectives()?.Any(d => _keyDirectivePredicate(d) && !d.Any(arg => arg.Name == RESOLVABLE_ARGUMENT && arg.Value is bool b && !b)) == true)
        {
            var entityType = schema.AllTypes["_Entity"] as UnionGraphType
                ?? throw new InvalidOperationException("The _Entity type is not defined in the schema.");
            entityType.AddPossibleType(type);
        }
    }

    public override void PostVisitSchema(ISchema schema)
    {
        var entityType = schema.AllTypes["_Entity"] as UnionGraphType
            ?? throw new InvalidOperationException("The _Entity type is not defined in the schema.");
        var anyScalarGraphType = schema.AllTypes["_Any"] as ScalarGraphType
            ?? throw new InvalidOperationException("The _Any scalar type is not defined in the schema.");
        if (entityType.PossibleTypes.Count == 0)
        {
            entityType.IsPrivate = true; // removes the _Entity type from the schema if no entities are resolvable
        }
        else
        {
            var representationsArgumentType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(anyScalarGraphType)));
            var representationsArgument = new QueryArgument(representationsArgumentType) { Name = "representations" };
            representationsArgument.Parser += (value) => EntityResolver.Instance.ConvertRepresentations(schema, (IList)value);
            schema.Query.AddField(new FieldType
            {
                Name = "_entities",
                ResolvedType = new NonNullGraphType(new ListGraphType(entityType)),
                Arguments = new QueryArguments(representationsArgument),
                Resolver = EntityResolver.Instance,
            });
        }
    }
}
