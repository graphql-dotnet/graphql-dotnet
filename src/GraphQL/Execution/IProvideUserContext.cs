namespace GraphQL.Execution
{
    /// <summary>
    /// Provides access to a mutable user-defined context for the duration of the query
    /// </summary>
    public interface IProvideUserContext
    {
        /// <summary>
        /// Mutable user-defined context to be passed to and shared by all field resolvers.
        /// <br/><br/>
        /// A custom implementation of <see cref="IDictionary{TKey, TValue}">IDictionary</see> may be
        /// used in place of the default <see cref="Dictionary{TKey, TValue}">Dictionary</see>.
        /// </summary>
        IDictionary<string, object?> UserContext { get; }
    }
}
