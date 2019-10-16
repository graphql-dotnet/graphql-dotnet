using System.Collections.Generic;

namespace GraphQL.Execution
{
    public interface IProvideUserContext
    {
        IDictionary<string, object> UserContext { get; }
    }
}
