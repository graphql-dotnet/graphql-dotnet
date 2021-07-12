#nullable enable

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
            ValuesList = (values ?? throw new ArgumentNullException(nameof(values))).ToList();
        }

        /// <summary>
        /// Initializes a new instance with the specified list of values.
        /// </summary>
        public ListValue(List<IValue> values)
        {
            ValuesList = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Returns a <see cref="List{T}">List&lt;object&gt;</see> containing the values of the list.
        /// </summary>
        public object Value
        {
            get
            {
                var list = new List<object?>(ValuesList.Count);
                foreach (var item in ValuesList)
                    list.Add(item.Value);
                return list;
            }
        }

        /// <summary>
        /// Returns a list of the child value nodes.
        /// </summary>
        public IEnumerable<IValue> Values => ValuesList;

        internal List<IValue> ValuesList { get; private set; }

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => ValuesList;

        /// <inheritdoc/>
        public override void Visit<TState>(Action<INode, TState> action, TState state)
        {
            foreach (var value in ValuesList)
                action(value, state);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string values = string.Join(", ", Values.Select(x => x.ToString()));
            return $"ListValue{{values={values}}}";
        }
    }
}
