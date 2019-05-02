using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class ObjectValue : AbstractNode, IValue
    {
        public ObjectValue(IEnumerable<ObjectField> fields)
        {
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

        public IEnumerable<string> FieldNames
        {
            get { return ObjectFields.Select(x => x.Name).ToList(); }
        }

        public override IEnumerable<INode> Children => ObjectFields;

        public ObjectField Field(string name)
        {
            return ObjectFields.FirstOrDefault(x => x.Name == name);
        }

        public override string ToString()
        {
            return "ObjectValue{{objectFields={0}}}".ToFormat(string.Join(", ", ObjectFields.Select(x => x.ToString())));
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return true;
        }
    }
}
