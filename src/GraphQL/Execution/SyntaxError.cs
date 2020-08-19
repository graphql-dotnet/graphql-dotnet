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
    }
}
