namespace GraphQL.Caching;

/// <summary>
/// Represents a cache of queries based on their hash.
/// </summary>
public interface IQueryCache : ICache<string>
{
}
