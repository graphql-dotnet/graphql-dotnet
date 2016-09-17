using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Types
{
    public interface IReferenceTarget
    {
        IGraphType Type { get; set; }
    }
}
