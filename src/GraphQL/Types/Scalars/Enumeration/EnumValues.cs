using System.Collections;
using GraphQLParser;

namespace GraphQL.Types
{
    public abstract class EnumValuesBase : IEnumerable<EnumValueDefinition>
    {
        /// <summary>
        /// Returns an enumeration definition for the specified name and <see langword="null"/> if not found.
        /// </summary>
        public EnumValueDefinition? this[string name] => FindByName(name);

        /// <summary>
        /// Gets the count of enumeration definitions.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Adds an enumeration definition to the set.
        /// </summary>
        /// <param name="value"></param>
        public abstract void Add(EnumValueDefinition value);

        /// <summary>
        /// Returns an enumeration definition for the specified name.
        /// </summary>
        public abstract EnumValueDefinition? FindByName(ROM name);

        /// <summary>
        /// Returns an enumeration definition for the specified value.
        /// </summary>
        public abstract EnumValueDefinition? FindByValue(object? value);

        public abstract IEnumerator<EnumValueDefinition> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A class that represents a set of enumeration definitions.
    /// </summary>
    public class EnumValues : EnumValuesBase
    {
        private List<EnumValueDefinition> List { get; } = new List<EnumValueDefinition>();

        /// <inheritdoc/>
        public override int Count => List.Count;

        /// <inheritdoc/>
        public override void Add(EnumValueDefinition value) => List.Add(value ?? throw new ArgumentNullException(nameof(value)));

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByName(ROM name)
        {
            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (def.Name == name)
                    return def;
            }

            return null;
        }

        /// <inheritdoc/>
        public override EnumValueDefinition? FindByValue(object? value)
        {
            if (value is Enum)
            {
                value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())); //ISSUE:allocation
            }

            // DO NOT USE LINQ ON HOT PATH
            foreach (var def in List)
            {
                if (Equals(def.UnderlyingValue, value))
                    return def;
            }

            return null;
        }

        public override IEnumerator<EnumValueDefinition> GetEnumerator() => List.GetEnumerator();
    }

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

        public override IEnumerator<EnumValueDefinition> GetEnumerator() => DictionaryByName.Values.GetEnumerator();
    }
}
