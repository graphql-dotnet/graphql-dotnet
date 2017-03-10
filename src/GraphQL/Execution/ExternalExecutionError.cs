using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class ExternalExecutionError : Exception
    {
        public ExternalExecutionError(string message) : base(message)
        {
            
        }
    }
}
