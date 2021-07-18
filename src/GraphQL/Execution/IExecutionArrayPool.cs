namespace GraphQL.Execution
{
    /// <summary>
    /// Provides a resource pool of temporary arrays during query execution.
    /// Can be used to return lists of data from field resolvers.
    /// </summary>
    public interface IExecutionArrayPool
    {
        /// <summary>
        /// Gets an array of the specified minimum length from the execution's array pool.
        /// This array will be returned to the pool once the execution completes. It is
        /// important that you do not use this array after execution; otherwise its
        /// contents may be overwritten at any point in time. This method is safe
        /// for multi-threaded operation.
        /// </summary>
        /// <typeparam name="TElement">Array element type.</typeparam>
        /// <param name="minimumLength">The minimum length of the array.</param>
        /// <returns>Array from pool.</returns>
        TElement[] Rent<TElement>(int minimumLength);
    }
}
