using System.Collections;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using static GraphQL.Federation.FederationHelper;

namespace GraphQL.Federation.Resolvers;

/// <summary>
/// Resolves the <c>_entities</c> field for GraphQL Federation.
/// </summary>
/// <remarks>
/// Be sure to parse the representations before calling this resolver, such as shown here:
/// <code>
/// representationArgument.Parser += (value) =&gt; EntityResolver.Instance.ConvertRepresentations(schema, (System.Collections.IList)value);
/// </code>
/// </remarks>
public sealed class EntityResolver : IFieldResolver
{
    /// <inheritdoc/>
    private EntityResolver()
    {
    }

    /// <summary>
    /// Returns the static instance of <see cref="EntityResolver"/>.
    /// </summary>
    public static EntityResolver Instance { get; } = new EntityResolver();

    /// <summary>
    /// Converts representations to a list of <see cref="Representation"/> objects.
    /// This should occur during field validation so that the representations can be validated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Occurs when the requested type cannot be found, is not an object graph type, has not been
    /// configured for GraphQL Federation, or cannot be converted to the source type.
    /// </exception>
    /// <remarks>
    /// Exceptions thrown within this method are expected to be returned to the caller as a validation error
    /// (aka Input Error), not logged as a server error (aka Processing Error).
    /// </remarks>
    public IEnumerable<Representation> ConvertRepresentations(ISchema schema, IList representations)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));
        if (representations == null)
            throw new ArgumentNullException(nameof(representations));

        // enumerate the requested representations, ensuring that the resulting list
        // returns the representations in the same order as the input list (spec requirement)
        var ret = new List<Representation>();
        foreach (var representation in representations)
        {
            // the representation should always be a dictionary, although the _Any scalar type
            // does not enforce that it is not a scalar or list
            if (representation is not IDictionary<string, object?> rep)
                throw new InvalidOperationException("Representation must be a dictionary.");

            // pull the __typename field from the representation, which will indicate the type
            if (!rep.TryGetValue("__typename", out var typeNameObj) || typeNameObj is not string typeName)
                throw new InvalidOperationException("Representation must contain a __typename field.");

            // now find the graph type instance for the type name, ensuring it is an object type and has an entity resolver
            var graphTypeInstance = schema.AllTypes[typeName]
                ?? throw new InvalidOperationException($"The type '{typeName}' could not be found.");
            if (graphTypeInstance is not IObjectGraphType objectGraphType)
                throw new InvalidOperationException($"The type '{typeName}' is not an object graph type.");
            var resolver = graphTypeInstance.GetMetadata<IFederationResolver>(RESOLVER_METADATA)
                ?? throw new InvalidOperationException($"The type '{typeName}' has not been configured for GraphQL Federation.");

            // each entity resolver defines (1) a method to parse the representation into an object, which occurs during
            //   the validation phase of the GraphQL execution, and (2) a resolver method to convert this object into the
            //   entity, which occurs during the execution phase of the GraphQL execution.
            object value;
            try
            {
                value = resolver.ParseRepresentation(objectGraphType, rep);
            }
            catch (Exception ex)
            {
                // mask the underlying exception to prevent leaking implementation details
                // the InnerException can be read for debugging purposes
                throw new InvalidOperationException($"Error converting representation for type '{typeName}'. Please verify your supergraph is up to date.", ex);
            }

            ret.Add(new Representation(objectGraphType, resolver, value));
        }
        return ret;
    }

    private class RepresentationDataLoader(IResolveFieldContext Context, Representation Representation) : IDataLoaderResult
    {
        public Task<object?> GetResultAsync(CancellationToken cancellationToken = default)
            => Representation.Resolver.ResolveAsync(Context, Representation.GraphType, Representation.Value).AsTask();
    }

    /// <inheritdoc/>
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        // require the representations argument to be converted to the proper type before hitting this code
        // e.g.: representationArgument.Parser += (value) => EntityResolver.Instance.ConvertRepresentations(schema, (System.Collections.IList)value);
        var representations = (IEnumerable<Representation>)context.Arguments![REPRESENTATIONS_ARGUMENT].Value!;

        // now that the representations have been validated, we can use them to resolve the entities using
        //   the resolvers provided by the representations

        // note: context.Copy is implicit due to the returned object being a list; otherwise it would be necessary,
        //   as the context is referenced within a delegate passed to the SimpleDataLoader (see below)
        //context = context.Copy();

        var results = new List<RepresentationDataLoader>();
        foreach (var representation in representations)
        {
            // using a data loader here causes the resolvers to run in serial or parallel based on the selected execution strategy.
            // unfortunately this requires extra allocations whereas if the strategy was known this code could be optimized by
            // either awaiting each resolver or collecting them and performing WaitAll. Note that this code counts on the fact
            // that the context instance will not be reused due to a list being returned from this method.
            results.Add(new RepresentationDataLoader(context, representation));
        }

        return new(results);
    }
}
