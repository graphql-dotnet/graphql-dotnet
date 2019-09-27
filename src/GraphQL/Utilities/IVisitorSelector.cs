using System.Collections.Generic;

namespace GraphQL.Utilities
{
    public interface IVisitorSelector
    {
        IEnumerable<ISchemaNodeVisitor> Select(object node);
    }
}
