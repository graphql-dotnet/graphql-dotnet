using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Abstractions
{
    public interface IHasUserContext
    {
        IDictionary<string, object> UserContext { get; }
    }
}
