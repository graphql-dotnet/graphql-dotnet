using GraphQL.Utilities;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an enumeration value (specified by a string) within a document.
    /// </summary>
    public class EnumValue : GraphQLEnumValue, IValue
    {
        /// <summary>
        /// Initializes a new instance with a <see cref="GraphQLName"/> containing a string representation of the enumeration value.
        /// </summary>
        public EnumValue(GraphQLName name)
        {
            NameValidator.ValidateDefault(name, NamedElement.EnumValue);
            Name = name;
            ClrValue = (string)name; //TODO:allocation
        }

        /// <summary>
        /// Initializes a new instance with a specified string representation of the enumeration value.
        /// </summary>
        public EnumValue(string name)
        {
            NameValidator.ValidateDefault(name, NamedElement.EnumValue);
            Name = new GraphQLName(name);
            ClrValue = name;
        }

        public string ClrValue { get; }

        object? IValue.ClrValue => ClrValue;
    }
}
