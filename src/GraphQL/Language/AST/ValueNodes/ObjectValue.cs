using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a complex value within a document that has child fields (an object).
    /// </summary>
    public class ObjectValue : AbstractNode, IValue
    {
        /// <summary>
        /// Initializes a new instance that contains the specified field nodes.
        /// </summary>
        public ObjectValue(IEnumerable<ObjectField> fields)
        {
            ObjectFields = fields ?? Array.Empty<ObjectField>();
        }

        /// <summary>
        /// Returns a <see cref="Dictionary{TKey, TValue}">Dictionary&lt;string, object&gt;</see>
        /// containing the values of the field nodes that this object value node contains.
        /// </summary>
        public object Value
        {
            get
            {
                var obj = new Dictionary<string, object>();
                FieldNames.Apply(name => obj.Add(name, Field(name).Value.Value));
                return obj;
            }
        }

        /// <summary>
        /// Returns the field value nodes that are contained within this object value node.
        /// </summary>
        public IEnumerable<ObjectField> ObjectFields { get; }

        /// <summary>
        /// Returns a list of the names of the fields specified for this object value node.
        /// </summary>
        public IEnumerable<string> FieldNames => ObjectFields.Select(x => x.Name).ToList();

        /// <inheritdoc/>
        public override IEnumerable<INode> Children => ObjectFields;

        /// <summary>
        /// Returns the first matching field node contained within this object value node that matches the specified name, or <see langword="null"/> otherwise.
        /// </summary>
        public ObjectField Field(string name)
        {
            return ObjectFields.FirstOrDefault(x => x.Name == name);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string fields = string.Join(", ", ObjectFields.Select(x => x.ToString()));
            return $"ObjectValue{{objectFields={fields}}}";
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

            return true;
        }
    }
}
