namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides configuration for specific field argument when building schema via <see cref="SchemaBuilder"/>.
    /// </summary>
    public class ArgumentConfig : MetadataProvider
    {
        /// <summary>
        /// Creates an instance of <see cref="ArgumentConfig"/> with the specified name.
        /// </summary>
        /// <param name="name">Field argument name.</param>
        public ArgumentConfig(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the argument description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default value of the field argument.
        /// </summary>
        public object? DefaultValue { get; set; }
    }
}
