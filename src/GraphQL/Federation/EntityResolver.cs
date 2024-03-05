using GraphQL.Resolvers;
using static GraphQL.Federation.Extensions.FederationHelper;

namespace GraphQL.Federation;

/// <summary>
/// Resolves the _entity field for GraphQL Federation.
/// </summary>
public class EntityResolver : IFieldResolver
{
    /// <summary>
    /// Returns the static instance of <see cref="EntityResolver"/>.
    /// </summary>
    public static EntityResolver Instance { get; } = new EntityResolver();

    /// <inheritdoc/>
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        // BUG: Authorization only works at a field level. Authorization at GraphType level doesn't work.
        //      i.e. _entities(representations: [{ __typename: "MyType", id: 1 }]) { myField }
        //      will only check authorization for "myField" but not for "MyType".
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
            var result = await resolver.ResolveAsync(context, rep).ConfigureAwait(false);
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
