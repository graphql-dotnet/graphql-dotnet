using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;

namespace GraphQL.Compilation
{
    public class CompiledNode
    {
        public CompiledNode(IGraphType graphType, Dictionary<string, CompiledField> fields)
        {
            GraphType = graphType;
            Fields = fields;
        }

        public Dictionary<string, CompiledField> Fields { get; }
        public IGraphType GraphType { get; }
    }
}
