using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    public class NonNullExecutionError : ExecutionError
    {
        public NonNullExecutionError(string message) : base(message)
        {
        }
    }
}
