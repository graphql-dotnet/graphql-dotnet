namespace GraphQL.Execution
{
    /// <summary>
    /// Provides capabilities to work with array pools.
    /// </summary>
    public interface IArrayPool
    {
        /// <summary>
        /// Rents an array of the specified minimum length from the pool. This array does not need
        /// to be returned to the pool yourself because it will be returned to the pool once the
        /// execution completes. It is important that you should not use this array after
        /// execution; otherwise, its contents will most likely be overwritten at any point in time.
        /// </summary>
        /// <typeparam name="TElement">Array element type.</typeparam>
        /// <param name="minimumLength">The minimum length of the array.</param>
        /// <returns>Array from pool.</returns>
        TElement[] Rent<TElement>(int minimumLength);
    }
}
