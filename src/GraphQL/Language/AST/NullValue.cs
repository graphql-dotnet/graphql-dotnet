using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Language.AST
{
    public class NullValue : AbstractNode, IValue
    {
        public override string ToString()
        {
            return "null";
        }

        public override bool IsEqualTo(INode obj)
        {
            if (ReferenceEquals(null, obj)) return true;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType();
        }
    }
}
