using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Validation
{
    public class ValidationContext
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        public string OperationName { get; set; }
        public ISchema Schema { get; set; }
        public Document Document { get; set; }

        public TypeInfo TypeInfo { get; set; }

        public IEnumerable<ValidationError> Errors
        {
            get { return _errors; }
        }

        public void ReportError(ValidationError error)
        {
            _errors.Add(error);
        }

        public string Print(INode node)
        {
            return AstPrinter.Print(node);
        }
    }
}
