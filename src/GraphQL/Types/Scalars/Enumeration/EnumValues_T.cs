using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a set of enumeration definitions
    /// corresponding to the desired .NET enum.
    /// </summary>
    public class EnumValues<TEnum> : EnumValuesBase where TEnum : Enum
    {
        private Dictionary<ROM, EnumValueDefinition> DictionaryByName { get; } = new();
        private Dictionary<TEnum, EnumValueDefinition> DictionaryByValue { get; } = new();

        /// <inheritdoc/>
        public override int Count => DictionaryByName.Count;

        /// <inheritdoc/>
        public override void Add(EnumValueDefinition value)
        {
            if (value.Value is not TEnum e)
                throw new ArgumentException($"Only values of {typeof(TEnum).Name} supported", nameof(value));

            DictionaryByName[value.Name] = value;
            DictionaryByValue[e] = value;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByName(ROM name)
        {
            return DictionaryByName.TryGetValue(name, out var def)
                ? def
                : null;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByValue(object? value)
        {
            // fast path
            if (value is TEnum e)
                return DictionaryByValue.TryGetValue(e, out var def) ? def : null;

            // slow path - for example from Serialize(int)
            foreach (var item in DictionaryByName)
            {
                if (Equals(item.Value.UnderlyingValue, value))
                    return item.Value;
            }

            return null;
        }

        /// <inheritdoc/>
        public override IEnumerator<EnumValueDefinition> GetEnumerator() => DictionaryByName.Values.GetEnumerator();
    }
}
