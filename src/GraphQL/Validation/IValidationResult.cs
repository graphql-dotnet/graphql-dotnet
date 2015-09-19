using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Validation
{
    public interface IValidationResult
    {
        bool IsValid { get; }
        ExecutionErrors Errors { get; set; }
    }
}
