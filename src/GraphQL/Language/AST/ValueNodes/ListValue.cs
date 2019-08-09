using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class ListValue : AbstractNode, IValue
    {
        public ListValue(IEnumerable<IValue> values)
        {
            Values = values;
        }

        public object Value
        {
            get
            {
                return Values.Select(x => x.Value).ToList();
            }
        }

        public IEnumerable<IValue> Values { get; }

        public override IEnumerable<INode> Children => Values;

        public override string ToString()
        {
            return "ListValue{{values={0}}}".ToFormat(string.Join(", ", Values.Select(x=>x.ToString())));
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
