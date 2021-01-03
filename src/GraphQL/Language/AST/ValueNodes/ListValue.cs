using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Language.AST
{
    public class ListValue : AbstractNode, IValue
    {
        public ListValue(IEnumerable<IValue> values)
        {
            Values = values ?? Array.Empty<IValue>();
        }

        public object Value => Values.Select(x => x.Value).ToList();

        public IEnumerable<IValue> Values { get; }

        public override IEnumerable<INode> Children => Values;

        /// <inheritdoc />
        public override string ToString()
        {
            string values = string.Join(", ", Values.Select(x => x.ToString()));
            return $"ListValue{{values={values}}}";
        }

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
