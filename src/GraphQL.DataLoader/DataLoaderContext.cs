namespace GraphQL.DataLoader
{
    /// <summary>
    /// Provides a way to register DataLoader instances
    /// </summary>
    public class DataLoaderContext
    {
        private readonly Dictionary<string, IDataLoader> _loaders = new Dictionary<string, IDataLoader>();

        /// <summary>
        /// Add a new data loader if one does not already exist with the provided key
        /// </summary>
        /// <typeparam name="TDataLoader">The type of <seealso cref="IDataLoader"/></typeparam>
        /// <param name="loaderKey">Unique string to identify the <seealso cref="IDataLoader"/> instance</param>
        /// <param name="dataLoaderFactory">Function to create the TDataLoader instance if it does not already exist</param>
        /// <returns>Returns an existing TDataLoader instance or a newly created instance if it did not exist already</returns>
        public TDataLoader GetOrAdd<TDataLoader>(string loaderKey, Func<TDataLoader> dataLoaderFactory)
            where TDataLoader : IDataLoader
        {
            if (loaderKey == null)
                throw new ArgumentNullException(nameof(loaderKey));

            if (dataLoaderFactory == null)
                throw new ArgumentNullException(nameof(dataLoaderFactory));

            IDataLoader loader;

            lock (_loaders)
            {
                if (!_loaders.TryGetValue(loaderKey, out loader))
                {
                    loader = dataLoaderFactory();

                    _loaders.Add(loaderKey, loader);
                }
            }

            return (TDataLoader)loader;
        }
    }
}
