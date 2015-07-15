using System.Collections.Generic;

namespace GraphQL.Types
{
    public class QueryArguments : List<QueryArgument>
    {
        public QueryArguments(IEnumerable<QueryArgument> list)
            : base(list)
        {
        }
    }
}
