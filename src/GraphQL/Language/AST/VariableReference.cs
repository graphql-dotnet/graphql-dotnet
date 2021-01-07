using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a value node that represents a reference to a variable within a document.
    /// </summary>
    public class VariableReference : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance with the specified <see cref="NameNode"/> containing the name of the variable being referenced.
        /// </summary>
        public VariableReference(NameNode name)
        {
            Name = name.Name;
            NameNode = name;
        }

        object IValue.Value => Name;

        /// <summary>
        /// Returns the name of the variable being referenced.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns a <see cref="NameNode"/> containing the name of the variable being referenced.
        /// </summary>
        public NameNode NameNode { get; }

        /// <inheritdoc/>
        public override string ToString() => $"VariableReference{{name={Name}}}";

        /// <summary>
        /// Compares this instance to another instance by comparing the name of the variable that is referenced.
        /// </summary>
        protected bool Equals(VariableReference other)
        {
            return string.Equals(Name, other.Name, StringComparison.InvariantCulture);
        }

        /// <inheritdoc/>
        public override bool IsEqualTo(INode obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((VariableReference)obj);
        }
    }
}
