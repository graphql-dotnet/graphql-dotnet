using GraphQLParser.Exceptions;

namespace GraphQL.Execution
{
    /// <summary>
    /// Represents an error generated while parsing the document.
    /// </summary>
    [Serializable]
    public class SyntaxError : DocumentError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxError"/> class from a specified
        /// <see cref="GraphQLSyntaxErrorException"/> instance, setting the <see cref="Exception.Message">Message</see>
        /// and <see cref="ExecutionError.Locations">Locations</see> properties appropriately.
        /// </summary>
        public SyntaxError(GraphQLSyntaxErrorException ex)
            : base("Error parsing query: " + ex.Description, ex)
        {
            // Code will contain SYNTAX_ERROR due to inner exception
            AddLocation(ex.Location);
        }
    }
}
