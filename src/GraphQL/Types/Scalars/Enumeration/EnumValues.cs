using GraphQLParser;

namespace GraphQL.Types
{
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

        /// <inheritdoc/>
        public override IEnumerator<EnumValueDefinition> GetEnumerator() => List.GetEnumerator();
    }
}
