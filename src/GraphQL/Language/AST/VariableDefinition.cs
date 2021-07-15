#nullable enable

using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a variable definition node within a document.
    /// </summary>
    public class VariableDefinition : AbstractNode, IHaveName
    {
        /// <summary>
        /// Initializes a new variable definition node with the specified <see cref="NameNode"/> containing the name of the variable.
        /// </summary>
        [Obsolete]
        public VariableDefinition(NameNode node) : this(node, null!)
        {
            NameNode = node;
        }

        /// <summary>
        /// Initializes a new variable definition node with the specified <see cref="NameNode"/> containing the name of the variable.
        /// </summary>
        public VariableDefinition(NameNode node, IType type)
        {
            NameNode = node;
#pragma warning disable CS0612 // Type or member is obsolete
            Type = type;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Returns the name of the variable.
        /// </summary>
        public string Name => NameNode.Name;

        /// <summary>
        /// Gets or sets the <see cref="NameNode"/> containing the name of the variable.
        /// </summary>
        public NameNode NameNode { get; }

        /// <summary>
        /// Returns the type node representing the graph type of the variable.
        /// </summary>
        public IType Type
        {
            get;
            [Obsolete]
            set;
        }

        /// <summary>
        /// Returns a value node representing the default value of the variable.
        /// Returns <see langword="null"/> if the variable has no default value.
        /// </summary>
        public IValue? DefaultValue { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                yield return Type;

                if (DefaultValue != null)
                    yield return DefaultValue;
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            action(Type, state);

            if (DefaultValue != null)
                action(DefaultValue, state);
        }

        /// <inheritdoc/>
        public override string ToString() => $"VariableDefinition{{name={Name},type={Type},defaultValue={DefaultValue}}}";
    }
}
