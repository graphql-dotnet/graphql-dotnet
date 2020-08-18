using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Execution
{
    [Serializable]
    public class InvalidOperationError : DocumentError
    {
        public InvalidOperationError(string message)
            : base(message)
        {
            Code = "INVALID_OPERATION";
        }
    }
}
