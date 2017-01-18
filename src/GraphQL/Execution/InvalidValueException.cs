using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Execution
{
    public class InvalidValueException : ExecutionError
    {
        public InvalidValueException(string fieldName, string message) : 
            base($"Variable '${fieldName}' is invalid. {message}")
        {
            
        }
    }
}
