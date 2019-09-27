using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.Utilities.Federation
{
    public class FederatedResolveContext
    {
        public ResolveFieldContext ParentFieldContext { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
    }
}
