using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{

    class LoneAnonymousOperationError : ValidationError
    {
        public LoneAnonymousOperationError(string message)
            : base(message)
        {
        }

        public LoneAnonymousOperationError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override string ErrorCode
        {
            get { return "5.1.2.1"; }
        }
    }

    class LoneAnonymousOperationRule : IValidationRule
    {
        public List<ExecutionError> Validate(Schema schema, Document document, string operationName)
        {

            if (string.IsNullOrWhiteSpace(operationName)
                && document.Operations.Count() > 1)
            {
                return new List<ExecutionError>( ){
                    new LoneAnonymousOperationError( "Must provide operation name if query contains multiple operations")
                };
            }
            return null;
        }
    }
}
