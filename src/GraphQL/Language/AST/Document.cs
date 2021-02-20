using System;
using System.Collections.Generic;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents the root node of a document.
    /// </summary>
    public class Document : AbstractNode
    {
        /// <summary>
        /// Initializes a new instance with no children.
        /// </summary>
        public Document()
        {
            Operations = new Operations();
            Fragments = new Fragments();
        }

        /// <summary>
        /// Gets or sets the query before being parsed into an AST document.
        /// </summary>
        public string OriginalQuery { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children
        {
            get
            {
                if (Operations.List != null)
                {
                    foreach (var o in Operations.List)
                        yield return o;
                }

                if (Fragments.List != null)
                {
                    foreach (var f in Fragments.List)
                        yield return f;
                }
            }
        }

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            if (Operations.List != null)
            {
                foreach (var definition in Operations.List)
                    action(definition, state);
            }

            if (Fragments.List != null)
            {
                foreach (var definition in Fragments.List)
                    action(definition, state);
            }
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
        public override string ToString() => $"Document{{definitions={string.Join(", ", Children)}}}";
    }
}
