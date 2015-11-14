using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQL.Validation.Rules
{

    class OperationNameUniquenessError : ValidationError
    {
        public OperationNameUniquenessError(string message) : base(message)
        {
        }

        public OperationNameUniquenessError(string message, Exception innerException) : base(message, innerException)
        {
        }

        public override string ErrorCode
        {
            get { return "5.1.1.1"; }
        }
    }

    /// <summary>
    /// Implements Language Specification 5.1.1.1
    /// </summary>
    class OperationNameUniquenessRule : IValidationRule
    {
        public List<ExecutionError> Validate(Schema schema, Document document, string operationName)
        {

            if (document.Operations.Count < 2)
            {
                return null;
            }

            List<ExecutionError> errors = new List<ExecutionError>(document.Operations.Count() / 2);
            IList<  string> operationNames = document.Operations.Select(op => op.Name).ToList() ;
            Dictionary<string, int?> frequency = new Dictionary<string, int?>(operationNames.Count());
            foreach (string name in operationNames)
            {
                int? existingCount = null;
                if (frequency.TryGetValue(name, out existingCount))
                {
                    frequency[name] = (existingCount ?? 0) + 1;
                }
                else
                {
                    frequency.Add(name, 1);
                }
            }
            foreach (KeyValuePair<string, int?> frequencies in frequency)
            {
                if (frequencies.Value.HasValue && frequencies.Value.Value > 1)
                {
                    errors.Add(new OperationNameUniquenessError( 
                        String.Format("Validation Error: Operation Name Uniqueness - '{0}' is used {1} times in the document. Specify unique names for each operation.", 
                        frequencies.Key, 
                        frequencies.Value.Value 
                        )));
                }
            }
            errors.TrimExcess();

            return errors; 
        }
    }
}
