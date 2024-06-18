using GraphQL.DI;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Visitors;

/// <summary>
/// This adds the <c>_service</c> field to the query type.
/// This cannot be added via an <see cref="IConfigureSchema"/> instance because the
/// <see cref="ISchema.Query"/> property will not yet be set. Requires the <c>_Service</c>,
/// type to have been already defined in the schema.
/// </summary>
/// <remarks>
/// Since the schema node visitors run after the schema has been initialized, we must
/// set the ResolvedType property of the fields to type pulled from the schema, rather
/// than setting the Type property directly. It is also not possible to use the
/// <see cref="GraphQLTypeReference"/> meta type to reference the types, as it is
/// normally resolved during schema initialization, which has already occurred.
/// </remarks>
internal class FederationServiceSchemaNodeVisitor : BaseSchemaNodeVisitor
{
    /// <inheritdoc/>
    public override void VisitSchema(ISchema schema)
    {
        var serviceType = schema.AllTypes["_Service"] as ObjectGraphType
            ?? throw new InvalidOperationException("The _Service type is not defined in the schema.");

        schema.Query.AddField(new FieldType
        {
            Name = "_service",
            ResolvedType = new NonNullGraphType(serviceType),
            Resolver = new FuncFieldResolver<object>(_ => BoolBox.True)
        });
    }
}
