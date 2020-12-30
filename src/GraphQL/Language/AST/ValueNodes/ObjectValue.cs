using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class ObjectValue : AbstractNode, IValue
    {
        public ObjectValue(IEnumerable<ObjectField> fields)
        {
            if (fields == null)
                ObjectFields = Array.Empty<ObjectField>();
            else
                ObjectFields = fields;
        }

        public object Value
        {
            get
            {
                var obj = new Dictionary<string, object>();
                FieldNames.Apply(name => obj.Add(name, Field(name).Value.Value));
                return obj;
            }
        }

        public IEnumerable<ObjectField> ObjectFields { get; }

        public IEnumerable<string> FieldNames => ObjectFields.Select(x => x.Name).ToList();

        public override IEnumerable<INode> Children => ObjectFields;

        public ObjectField Field(string name)
        {
            return ObjectFields.FirstOrDefault(x => x.Name == name);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            string fields = string.Join(", ", ObjectFields.Select(x => x.ToString()));
            return $"ObjectValue{{objectFields={fields}}}";
        }

        public override bool IsEqualTo(INode obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return true;
        }
    }
}
