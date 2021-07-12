#nullable enable

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
            NameNode = name;
        }

        object IValue.Value => Name;

        /// <summary>
        /// Returns the name of the variable being referenced.
        /// </summary>
        public string Name => NameNode.Name!;

        /// <summary>
        /// Returns a <see cref="NameNode"/> containing the name of the variable being referenced.
        /// </summary>
        public NameNode NameNode { get; }

        /// <inheritdoc/>
        public override string ToString() => $"VariableReference{{name={Name}}}";
    }
}
