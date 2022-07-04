namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides access to a <seealso cref="DataLoaderContext"/>
    /// </summary>
    public interface IDataLoaderContextAccessor
    {
        /// <summary>
        /// The current <seealso cref="DataLoaderContext"/>
        /// </summary>
        DataLoaderContext? Context { get; set; }
    }
}
