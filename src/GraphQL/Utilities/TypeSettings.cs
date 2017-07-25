using System;

namespace GraphQL.Utilities
{
    public class TypeSettings
    {
        private readonly LightweightCache<string, TypeConfig> _typeConfigurations;

        public TypeSettings()
        {
            _typeConfigurations = new LightweightCache<string, TypeConfig>(s => new TypeConfig(s));
        }

        public void Configure(string typeName, Action<TypeConfig> configure)
        {
            var config = _typeConfigurations[typeName];
            configure(config);
        }

        public TypeConfig ConfigFor(string typeName)
        {
            return _typeConfigurations[typeName];
        }
    }
}