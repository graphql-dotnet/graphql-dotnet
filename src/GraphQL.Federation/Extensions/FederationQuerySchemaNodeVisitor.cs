using GraphQL.Federation.Types;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation.Extensions;

internal class FederationQuerySchemaNodeVisitor : BaseSchemaNodeVisitor
{
    private readonly ServiceGraphType _serviceType;
    private readonly EntityType _entityType;


    public FederationQuerySchemaNodeVisitor(ServiceGraphType serviceType, EntityType entityType)
    {
        _serviceType = serviceType;
        _entityType = entityType;
    }


    public override void VisitObject(IObjectGraphType type, ISchema schema)
    {
        if (type == schema.Query)
        {
            type.AddField(new FieldType
            {
                Name = "_service",
                ResolvedType = new NonNullGraphType(_serviceType),
                Resolver = new FuncFieldResolver<object>(context => new { })
            });

            var representationsType = new NonNullGraphType(new ListGraphType(new NonNullGraphType(new Utilities.Federation.AnyScalarGraphType())));
            type.AddField(new FieldType
            {
                Name = "_entities",
                ResolvedType = new NonNullGraphType(new ListGraphType(_entityType)),
                Arguments = new QueryArguments(
                    new QueryArgument(representationsType) { Name = "representations" }
                ),
                Resolver = new FuncFieldResolver<object>(ResolveEntities)
            });
        }
    }

    // BUG: Authorization only works at a field level. Authorization at GraphType level doesn't work.
    //      i.e. _entities(representations: [{ __typename: "MyType", id: 1 }]) { myField }
    //      will only check authorization for "myField" but not for "MyType".
    public static object ResolveEntities(IResolveFieldContext context)
    {
        var repMaps = context.GetArgument<List<Dictionary<string, object>>>("representations");
        var results = new List<object?>();
        foreach (var repMap in repMaps)
        {
            var typeName = repMap["__typename"].ToString();
            var graphTypeInstance = context.Schema.AllTypes[typeName]!;
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA);
            if (resolver == null)
            {
                throw new NotImplementedException($"ResolveReference() was not provided for {graphTypeInstance.Name}.");
            }

            var rep = repMap!.ToObject(resolver.SourceType);
            var result = resolver.Resolve(context, rep);
            results.Add(result);
        }
        return results;
    }
}
