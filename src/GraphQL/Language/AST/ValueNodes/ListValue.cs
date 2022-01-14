using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a list of value nodes, expressed as a value node itself, within a document.
    /// </summary>
    public class ListValue : GraphQLListValue, IValue
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
        public object ClrValue
        {
            get
            {
                var list = new List<object?>(ValuesList.Count);
                foreach (var item in ValuesList)
                    list.Add(item.ClrValue);
                return list;
            }
        }

        internal List<IValue> ValuesList { get; private set; }
    }
}
