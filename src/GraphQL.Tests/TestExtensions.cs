using System;

namespace GraphQL.Tests
{
    internal static class TestExtensions
    {
        public static LightDictionary ToLightDictionary(this object data)
        {
            if (data == null)
                return LightDictionary.Empty;

            return data is ObjectProperty[] properties
                ? new LightDictionary(properties)
                : throw new ArgumentException($"Unknown type {data.GetType()}. Parameter must be of type ObjectProperty[].", nameof(data));
        }
    }

    /// <summary>
    /// Lightweight analog for dictionary.
    /// </summary>
    internal struct LightDictionary
    {
        private readonly ObjectProperty[] _properties;

        public static readonly LightDictionary Empty = new LightDictionary();

        public LightDictionary(ObjectProperty[] properties)
        {
            _properties = properties;
        }

        public int Count => _properties?.Length ?? 0;

        /// <summary>
        /// Getsa property value by its key (name).
        /// </summary>
        /// <param name="key">Property name.</param>
        /// <returns>Property value if exist.</returns>
        public object this[string key]
        {
            get
            {
                if (_properties?.Length > 0)
                {
                    foreach (var property in _properties)
                    {
                        if (property.Key == key)
                            return property.Value;
                    }
                }

                return null;
            }
        }
    }
}
