using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Federation.Extensions;

internal class FederationQuerySchemaNodeVisitor : BaseSchemaNodeVisitor
{
    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        if (type == schema.Query)
        {
            var serviceType = schema.AllTypes["_Service"] as ObjectGraphType
                ?? throw new InvalidOperationException("The _Service type is not defined in the schema.");
            var entityType = schema.AllTypes["_Entity"] as UnionGraphType
                ?? throw new InvalidOperationException("The _Entity type is not defined in the schema.");
            type.AddField(new FieldType
            {
                Name = "_service",
                ResolvedType = new NonNullGraphType(serviceType),
                Resolver = new FuncFieldResolver<object>(_ => BoolBox.True)
            });

            var representationsType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(new Utilities.Federation.AnyScalarGraphType())));
            type.AddField(new FieldType
            {
                Name = "_entities",
                ResolvedType = new NonNullGraphType(new ListGraphType(entityType)),
                Arguments = new QueryArguments(
                    new QueryArgument(representationsType) { Name = "representations" }
                ),
                Resolver = EntityResolver.Instance,
            });
        }
    }
}
