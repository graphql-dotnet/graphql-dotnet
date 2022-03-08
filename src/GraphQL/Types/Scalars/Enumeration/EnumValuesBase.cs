using System.Collections;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// Base class for collection of enumeration values used be <see cref="EnumerationGraphType"/>.
    /// </summary>
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

        /// <inheritdoc/>
        public abstract IEnumerator<EnumValueDefinition> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
