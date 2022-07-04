namespace GraphQL.Types
{
    /// <summary>
    /// Provides basic capabilities for getting and setting arbitrary meta information.
    /// This interface is implemented by numerous descendants like <see cref="GraphType"/>,
    /// <see cref="FieldType"/>, <see cref="Schema"/> or others.
    /// </summary>
    public interface IProvideMetadata
    {
        /// <summary>
        /// Provides all meta information as a key-value dictionary.
        /// </summary>
        Dictionary<string, object?> Metadata { get; }

        /// <summary>
        /// Gets a value by a given key. If there is no value for the given key, returns <paramref name="defaultValue"/>.
        /// </summary>
        /// <typeparam name="TType"> Type of the value. </typeparam>
        /// <param name="key"> String key. </param>
        /// <param name="defaultValue"> It is used if there is no value for the given key. </param>
        /// <returns> Value of the specified type. </returns>
        TType GetMetadata<TType>(string key, TType defaultValue = default!);

        /// <summary>
        /// Gets a value by a given key. If there is no value for the given key, returns value obtained from <paramref name="defaultValueFactory"/>.
        /// </summary>
        /// <typeparam name="TType"> Type of the value. </typeparam>
        /// <param name="key"> String key. </param>
        /// <param name="defaultValueFactory"> It is used if there is no value for the given key. </param>
        /// <returns> Value of the specified type. </returns>
        TType GetMetadata<TType>(string key, Func<TType> defaultValueFactory);

        /// <summary>
        /// Indicates whether there is meta information with the given key.
        /// </summary>
        /// <param name="key"> String key. </param>
        /// <returns> <c>true</c> if value for such key exists, otherwise <c>false</c>. </returns>
        bool HasMetadata(string key);
    }
}
