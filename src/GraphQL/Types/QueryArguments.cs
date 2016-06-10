using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class QueryArguments : List<QueryArgument>
    {
        public QueryArguments(params QueryArgument[] args)
        {
            AddRange(args);
        }

        public QueryArguments(IEnumerable<QueryArgument> list)
            : base(list)
        {
        }

        public QueryArgument Find(string name)
        {
            return this.FirstOrDefault(x => x.Name == name);
        }
    }
}
