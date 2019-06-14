using GraphQL.Execution;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.Validation
{
    public interface IFieldValidator
    {
        ExecutionErrors Validate(ISchema schema, IReadOnlyList<ValidationFrame> fieldStack);
    }
}
