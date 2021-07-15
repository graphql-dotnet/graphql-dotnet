#nullable enable

using System.Collections.Generic;

namespace GraphQL.Utilities.Federation
{
    public class FederatedResolveContext
    {
        public IResolveFieldContext ParentFieldContext { get; set; } = null!;
        public Dictionary<string, object?> Arguments { get; set; } = null!;
    }
}
