namespace GraphQL.Utilities
{
    /// <summary>
    /// Provides configuration for GraphTypes and their fields and arguments when building schema via <see cref="SchemaBuilder"/>.
    /// </summary>
    public class TypeSettings
    {
        private readonly LightweightCache<string, TypeConfig> _typeConfigurations;
        private readonly List<Action<TypeConfig>> _forAllTypesConfigurationDelegates;

        public TypeSettings()
        {
            _typeConfigurations = new LightweightCache<string, TypeConfig>(name => new TypeConfig(name));
            _forAllTypesConfigurationDelegates = new List<Action<TypeConfig>>();
        }

        /// <summary>
        /// Gets configuration for specific GraphType by its name.
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

        public TypeSettings ForAll(Action<TypeConfig> configure)
        {
            _forAllTypesConfigurationDelegates.Add(configure ?? throw new ArgumentNullException(nameof(configure)));
            return this;
        }

        public void Include<TType>()
        {
            Include(typeof(TType));
        }

        public void Include<TType>(string name)
        {
            Include(name, typeof(TType));
        }

        public void Include(Type type)
        {
            var name = type.GraphQLName();
            Include(name, type);
        }

        public void Include(string name, Type type)
        {
            _typeConfigurations[name].Type = type;
        }

        public void Include<TType, TTypeOfType>()
        {
            Include(typeof(TType), typeof(TTypeOfType));
        }

        public void Include<TType, TTypeOfType>(string name)
        {
            Include(name, typeof(TType), typeof(TTypeOfType));
        }

        public void Include(Type type, Type typeOfType)
        {
            var name = (type ?? throw new ArgumentNullException(nameof(type))).GraphQLName();
            Include(name, type, typeOfType);
        }

        public void Include(string name, Type type, Type typeOfType)
        {
            var config = _typeConfigurations[name];
            config.Type = type;
            config.IsTypeOfFunc = obj => obj?.GetType().IsAssignableFrom(typeOfType) ?? false;
        }
    }
}
