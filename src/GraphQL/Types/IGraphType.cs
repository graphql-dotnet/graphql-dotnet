using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Types
{
    public interface IGraphType
    {
        string Name { get; }
        string Description { get; }
        string DeprecationReason { get; }

        string CollectTypes(TypeCollectionContext context);
    }

    public interface IOutputGraphType : IGraphType
    {
    }

    public interface IInputGraphType : IGraphType
    {
    }
}
