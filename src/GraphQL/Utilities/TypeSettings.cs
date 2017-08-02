using System;

namespace GraphQL.Utilities
{
    public class TypeSettings
    {
        private readonly LightweightCache<string, TypeConfig> _typeConfigurations;

        public TypeSettings()
        {
            _typeConfigurations =
                new LightweightCache<string, TypeConfig>(name => new TypeConfig(name));
        }

        public void Configure(string typeName, Action<TypeConfig> configure)
        {
            var config = _typeConfigurations[typeName];
            configure(config);
        }

        public TypeConfig For(string typeName)
        {
            return _typeConfigurations[typeName];
        }

        public void Include<T>()
        {
            var type = typeof(T);
            var name = type.GraphQLName();
            Include(name, type);
        }

        public void Include(string name, Type type)
        {
            _typeConfigurations[name].Type = type;
        }
    }
}