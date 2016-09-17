using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public abstract class WrappingGraphType : GraphType, IReferenceTarget
    {
        public IGraphType Type { get; set; }
    }
}
