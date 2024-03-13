using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a record class for holding the necessary components to resolve an entity
/// in a GraphQL federation setup. This includes the GraphQL object type, the resolver,
/// and the converted representation of the entity.
/// </summary>
/// <param name="GraphType">The GraphQL object graph type associated with the entity being resolved.
/// This defines the shape of the output data for the GraphQL query.</param>
/// <param name="Resolver">The federation resolver responsible for resolving the entity.
/// Each entity type has its specific implementation of <see cref="IFederationResolver"/>
/// to handle its resolution logic.</param>
/// <param name="Value">The representation of the entity, converted to the CLR type as specified by
/// <see cref="IFederationResolver.SourceType"/>. This is the actual data passed to the resolver
/// for processing and fetching the corresponding entity.</param>
public record class Representation(IObjectGraphType GraphType, IFederationResolver Resolver, object Value);
