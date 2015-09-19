using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Validation
{
    abstract class ValidationError : ExecutionError
    {
        
        public abstract string ErrorCode { get; }
        
        public ValidationError(string message) : base(message)
        {
        }

        public ValidationError(string message, Exception innerException) : base(message, innerException)
        {
        }

        
    }
}
