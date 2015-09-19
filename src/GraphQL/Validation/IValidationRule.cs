using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation
{
    public interface IValidationRule
    {
        /// <summary>
        /// Returns one or more ExecutionError's for a given Schema, Document, and operationName
        /// </summary>
        /// <param name="schema">The Schema to validate</param>
        /// <param name="document">The document to validate</param>
        /// <param name="operationName">The operation name being validated</param>
        /// <returns></returns>
        List<ExecutionError> Validate(Schema schema, Document document, string operationName);
    }
}
