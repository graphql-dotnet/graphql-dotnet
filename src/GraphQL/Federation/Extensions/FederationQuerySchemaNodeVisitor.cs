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
            var graphTypeInstance = context.Schema.AllTypes[typeName]
                ?? throw new InvalidOperationException($"Type '{typeName}' not found.");
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                ?? throw new NotImplementedException($"ResolveReference() was not provided for {graphTypeInstance.Name}.");

            resolver.SourceGraphType ??= GenerateGraphType(resolver.SourceType, (IComplexGraphType)graphTypeInstance);
            var rep = repMap!.ToObject(resolver.SourceType, resolver.SourceGraphType);
            var result = resolver.Resolve(context, rep);
            results.Add(result);
        }
        return results;
    }

    private static IInputObjectGraphType GenerateGraphType(Type clrType, IComplexGraphType graphType)
    {
        var fields = graphType.GetAppliedDirectives()
            ?.Where(x => x.Name == KEY_DIRECTIVE)
            .Select(x => (string?)x.FindArgument(FIELDS_ARGUMENT)?.Value)
            .Where(fields => fields != null)
            .Select(fields => fields!.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            .SelectMany(x => x)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (fields == null || fields.Count == 0)
            throw new InvalidOperationException($"No keys specified for type '{graphType.Name}'.");

        var inputType = clrType == typeof(object)
            ? new InputObjectGraphType()
            : (IInputObjectGraphType)Activator.CreateInstance(typeof(InputObjectGraphType<>).MakeGenericType(clrType))!;

        foreach (var field in fields)
        {
            var originalField = graphType.GetField(field)
                ?? throw new InvalidOperationException($"Could not find field '{field}' on type '{graphType.Name}'.");

            inputType.AddField(new FieldType
            {
                Name = field,
                ResolvedType = originalField.ResolvedType!.IsInputType()
                    ? originalField.ResolvedType // scalar
                    : GenerateInputObjectGraphType((IComplexGraphType)originalField.ResolvedType!)
            });
        }

        return inputType;

        static InputObjectGraphType GenerateInputObjectGraphType(IComplexGraphType complexGraphType)
        {
            var ret = new InputObjectGraphType();
            foreach (var field in complexGraphType.Fields)
            {
                ret.AddField(new FieldType
                {
                    Name = field.Name,
                    ResolvedType = field.ResolvedType!.IsInputType()
                        ? field.ResolvedType
                        : GenerateInputObjectGraphType((IComplexGraphType)field.ResolvedType!),
                });
            }
            return ret;
        }
    }
}
