using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents an enumeration value (specified by a string) within a document.
    /// </summary>
    public class EnumValue : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance with a <see cref="NameNode"/> containing a string representation of the enumeration value.
        /// </summary>
        public EnumValue(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        /// <summary>
        /// Initializes a new instance with a specified string representation of the enumeration value.
        /// </summary>
        public EnumValue(string name)
        {
            Name = name;
        }

        object IValue.Value => Name;

        /// <summary>
        /// Returns the string representation of the enumeration value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns a <see cref="NameNode"/> containing the string representation of the enumeration value.
        /// </summary>
        public NameNode NameNode { get; }

        /// <inheritdoc/>
        public override string ToString() => $"EnumValue{{name={Name}}}";

        /// <summary>
        /// Compares this instance to another instance by comparing the string representation of the enumeration value.
        /// </summary>
        protected bool Equals(EnumValue other) => string.Equals(Name, other.Name, StringComparison.InvariantCulture);

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((EnumValue)obj);
        }
    }
}
