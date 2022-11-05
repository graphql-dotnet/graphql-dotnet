using System.Diagnostics;
using GraphQL.Utilities;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents an enumeration definition.
    /// </summary>
    [DebuggerDisplay("{Name}: {Value}")]
    public class EnumValueDefinition : MetadataProvider, IProvideDescription, IProvideDeprecationReason
    {
        /// <summary>
        /// Initializes a new instance with the specified name and value.
        /// </summary>
        public EnumValueDefinition(string name, object? value)
        {
            Name = name;
            Value = value;
            UnderlyingValue = value is Enum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()))
                : value;
        }

        /// <summary>
        /// The name of the enumeration member, as exposed through the GraphQL endpoint (e.g. "RED").
        /// </summary>
        public string Name { get; set; } //TODO: important to set this property only BEFORE EnumValuesBase.AddValue called

        /// <summary>
        /// A description of the enumeration member.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The reason this enumeration member has been deprecated; <see langword="null"/> if this member has not been deprecated.
        /// </summary>
        public string? DeprecationReason
        {
            get => this.GetDeprecationReason();
            set => this.SetDeprecationReason(value);
        }

        /// <summary>
        /// The value of the enumeration member, as referenced by the code (e.g. <see cref="ConsoleColor.Red"/>).
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// When mapped to a member of an <see cref="Enum"/>, contains the underlying enumeration value; otherwise contains <see cref="Value" />.
        /// </summary>
        internal object? UnderlyingValue { get; }
    }
}
