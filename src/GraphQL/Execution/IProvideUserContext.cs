using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    public interface IProvideUserContext
    {
        IDictionary<string, object> UserContext { get; }
    }
}
