namespace GraphQL.DI
{
    /// <summary>
    /// Provides a default implementation of <typeparamref name="T"/> for use when a specific
    /// implementation is not registered within the DI framework. 
    /// </summary>
    public interface IDefaultService<out T> where T : class
    {
        /// <summary>
        /// The default instance of the service.
        /// </summary>
        public T Instance { get; }
    }
}
