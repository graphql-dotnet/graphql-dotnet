using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Validation
{
    public class ValidationFrame
    {
        public ValidationFrame(string name, object value, IGraphType type, int? index)
        {
            Name = name;
            Value = value;
            GraphType = type;
            Index = index;
        }

        public string Name { set; get; }
        public object Value { set; get; }
        public IGraphType GraphType { set; get; }
        public int? Index { set; get; }

        public string GetLocation()
        {
            if (Index.HasValue)
                return $"{Name}[{Index}]";
            return Name;
        }
    }
}
