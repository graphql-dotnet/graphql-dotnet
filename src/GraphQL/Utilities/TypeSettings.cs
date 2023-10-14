namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides configuration for GraphTypes and their fields and arguments when building schema via <see cref="SchemaBuilder"/>.
    /// </summary>
    public class TypeSettings
    {
        private readonly LightweightCache<string, TypeConfig> _typeConfigurations;
        private readonly List<Action<TypeConfig>> _forAllTypesConfigurationDelegates;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public TypeSettings()
        {
            _typeConfigurations = new LightweightCache<string, TypeConfig>(name => new TypeConfig(name));
            _forAllTypesConfigurationDelegates = new List<Action<TypeConfig>>();
        }

        /// <summary>
        /// Gets configuration for specific GraphType by its name.
        /// Executes configured configuration delegates for the type cofiguration.
        /// </summary>
        /// <param name="typeName">Name of the GraphType.</param>
        public TypeConfig For(string typeName)
        {
            var exists = _typeConfigurations.Has(typeName);
            var typeConfig = _typeConfigurations[typeName];
            if (!exists)
            {
                foreach (var a in _forAllTypesConfigurationDelegates)
                    a.Invoke(typeConfig);
            }

            return typeConfig;
        }

        /// <summary>
        /// Adds a configuration delegate that executes for all types.
        /// </summary>
        public TypeSettings ForAll(Action<TypeConfig> configure)
        {
            _forAllTypesConfigurationDelegates.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
            return this;
        }

        /// <summary>
        /// Adds a configuration for the specified CLR type.
        /// </summary>
        public void Include<TType>()
        {
            Include(typeof(TType));
        }

        /// <summary>
        /// Adds a configuration for the specified CLR type, as the specified graph type name.
        /// </summary>
        public void Include<TType>(string name)
        {
            Include(name, typeof(TType));
        }

        /// <summary>
        /// Adds a configuration for the specified CLR type.
        /// </summary>
        public void Include(Type type)
        {
            var name = type.GraphQLName();
            Include(name, type);
        }

        /// <summary>
        /// Adds a configuration for the specified CLR type, as the specified graph type name.
        /// </summary>
        public void Include(string name, Type type)
        {
            _typeConfigurations[name].Type = type;
        }

        /// <summary>
        /// Adds a configuration for the specified CLR source type <typeparamref name="TTypeOfType"/>,
        /// which executes field resolvers on the specified CLR type <typeparamref name="TType"/>.
        /// </summary>
        public void Include<TType, TTypeOfType>()
        {
            Include(typeof(TType), typeof(TTypeOfType));
        }

        /// <summary>
        /// Adds a configuration for the specified CLR source type <typeparamref name="TTypeOfType"/>,
        /// which executes field resolvers on the specified CLR type <typeparamref name="TType"/>,
        /// with the specified graph type name.
        /// </summary>
        public void Include<TType, TTypeOfType>(string name)
        {
            Include(name, typeof(TType), typeof(TTypeOfType));
        }

        /// <summary>
        /// Adds a configuration for the specified CLR source type <paramref name="typeOfType"/>,
        /// which executes field resolvers on the specified CLR type <paramref name="type"/>.
        /// </summary>
        public void Include(Type type, Type typeOfType)
        {
            var name = (type ?? throw new ArgumentNullException(nameof(type))).GraphQLName();
            Include(name, type, typeOfType);
        }

        /// <summary>
        /// Adds a configuration for the specified CLR source type <paramref name="typeOfType"/>,
        /// which executes field resolvers on the specified CLR type <paramref name="type"/>,
        /// with the specified graph type name.
        /// </summary>
        public void Include(string name, Type type, Type typeOfType)
        {
            var config = _typeConfigurations[name];
            config.Type = type;
            config.IsTypeOfFunc = obj => obj?.GetType().IsAssignableFrom(typeOfType) ?? false;
        }
    }
}
