using System;
using GraphQLParser.Exceptions;

namespace GraphQL.Execution
{
    [Serializable]
    public class SyntaxError : DocumentError
    {
        public SyntaxError(GraphQLSyntaxErrorException ex)
            : base("Error parsing query: " + ex.Description, ex)
        {
            // Code will contain SYNTAX_ERROR due to inner exception
            AddLocation(ex.Line, ex.Column);
        }

        // available for use with third-party parsing engines
        public SyntaxError(string message, Exception innerException)
            : base("Error parsing query: " + message, innerException)
        {
            // the inner exception is of an unknown type, so set the code
            Code = "SYNTAX_ERROR";
        }
    }
}
