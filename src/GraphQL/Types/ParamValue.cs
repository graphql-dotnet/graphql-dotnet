using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Types
{
    public class ParamValue
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public IGraphType ResolvedType { get; set; }
    }
}
