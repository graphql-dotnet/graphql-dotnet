using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Utilities;
using static GraphQL.Federation.Extensions.FederationHelper;

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
        var results = new List<object>();
        foreach (var repMap in repMaps)
        {
            var typeName = repMap["__typename"].ToString();
            var graphTypeInstance = context.Schema.AllTypes[typeName]!;
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                ?? throw new NotImplementedException($"ResolveReference() was not provided for {graphTypeInstance.Name}.");

            //var rep = repMap!.ToObject(resolver.SourceType, null!);
            var rep = ToObject(resolver.SourceType, repMap);
            var result = resolver.Resolve(context, rep);
            results.Add(result);
        }
        return results;

        object ToObject(Type t, Dictionary<string, object> map)
        {
            var obj = Activator.CreateInstance(t)!;
            foreach (var item in map)
            {
                if (item.Key != "__typename")
                {
                    var prop = t.GetProperty(item.Key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public)
                        ?? throw new InvalidOperationException($"Property '{item.Key}' not found in type '{t.GetFriendlyName()}'.");
                    prop.SetValue(obj, item.Value);
                }
            }
            return obj;
        }
    }
}
