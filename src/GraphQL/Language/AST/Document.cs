using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the root node of a document.
    /// </summary>
    public class Document : AbstractNode
    {
        private readonly List<IDefinition> _definitions;

        /// <summary>
        /// Initializes a new instance with no children.
        /// </summary>
        public Document()
        {
            _definitions = new List<IDefinition>();
            Operations = new Operations();
            Fragments = new Fragments();
        }

        /// <summary>
        /// Gets or sets the query before being parsed into an AST document.
        /// </summary>
        public string OriginalQuery { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => _definitions;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            foreach (var definition in _definitions)
                action(definition, state);
        }

        /// <summary>
        /// Returns a list of operation nodes for this document.
        /// </summary>
        public Operations Operations { get; }

        /// <summary>
        /// Returns a list of fragment nodes for this document.
        /// </summary>
        public Fragments Fragments { get; }

        /// <summary>
        /// Adds a <see cref="FragmentDefinition"/> or <see cref="Operation"/> node to this document.
        /// </summary>
        public void AddDefinition(IDefinition definition)
        {
            _definitions.Add(definition ?? throw new ArgumentNullException(nameof(definition)));

            if (definition is FragmentDefinition fragmentDefinition)
            {
                Fragments.Add(fragmentDefinition);
            }
            else if (definition is Operation operation)
            {
                Operations.Add(operation);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(definition), $"Unhandled document definition '{definition.GetType().Name}'");
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"Document{{definitions={string.Join(", ", _definitions)}}}";
    }
}
