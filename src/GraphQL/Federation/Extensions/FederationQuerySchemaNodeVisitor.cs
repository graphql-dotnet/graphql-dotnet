using System.Collections;
using GraphQL.DI;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Extensions;

/// <summary>
/// This adds the <c>_service</c> and <c>_entities</c> fields to the query type.
/// This cannot be added via an <see cref="IConfigureSchema"/> instance because the
/// <see cref="ISchema.Query"/> property will not yet be set. Requires the <c>_Service</c>,
/// <c>_Entity</c> and <c>_Any</c> types to have been already defined in the schema.
/// </summary>
/// <remarks>
/// Since the schema node visitors run after the schema has been initialized, we must
/// set the ResolvedType property of the fields to type pulled from the schema, rather
/// than setting the Type property directly. It is also not possible to use the
/// <see cref="GraphQLTypeReference"/> meta type to reference the types, as it is
/// normally resolved during schema initialization, which has already occurred.
/// </remarks>
internal class FederationQuerySchemaNodeVisitor : BaseSchemaNodeVisitor
{
    /// <inheritdoc/>
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        if (type == schema.Query)
        {
            var serviceType = schema.AllTypes["_Service"] as ObjectGraphType
                ?? throw new InvalidOperationException("The _Service type is not defined in the schema.");
            var entityType = schema.AllTypes["_Entity"] as UnionGraphType
                ?? throw new InvalidOperationException("The _Entity type is not defined in the schema.");
            var anyScalarGraphType = schema.AllTypes["_Any"] as ScalarGraphType
                ?? throw new InvalidOperationException("The _Any scalar type is not defined in the schema.");

            type.AddField(new FieldType
            {
                Name = "_service",
                ResolvedType = new NonNullGraphType(serviceType),
                Resolver = new FuncFieldResolver<object>(_ => BoolBox.True)
            });

            var representationsArgumentType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(anyScalarGraphType)));
            var representationsArgument = new QueryArgument(representationsArgumentType) { Name = "representations" };
            representationsArgument.Parser += (value) => EntityResolver.Instance.ConvertRepresentations(schema, (IList)value);
            type.AddField(new FieldType
            {
                Name = "_entities",
                ResolvedType = new NonNullGraphType(new ListGraphType(entityType)),
                Arguments = new QueryArguments(representationsArgument),
                Resolver = EntityResolver.Instance,
            });
        }
    }
}
