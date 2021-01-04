using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of value nodes, expressed as a value node itself, within a document.
    /// </summary>
    public class ListValue : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance with the specified list of values.
        /// </summary>
        public ListValue(IEnumerable<IValue> values)
        {
            Values = values ?? Array.Empty<IValue>();
        }

        /// <summary>
        /// Returns a <see cref="List{T}">List&lt;object&gt;</see> containing the values of the list.
        /// </summary>
        public object Value => Values.Select(x => x.Value).ToList();

        /// <summary>
        /// Returns a list of the child value nodes.
        /// </summary>
        public IEnumerable<IValue> Values { get; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => Values;

        /// <inheritdoc/>
        public override string ToString()
        {
            string values = string.Join(", ", Values.Select(x => x.ToString()));
            return $"ListValue{{values={values}}}";
        }
    }
}
