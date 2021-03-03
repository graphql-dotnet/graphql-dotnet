using GraphQL.Language.AST;
namespace GraphQL.Execution
{
    /// <summary>
    /// Contains the parse error found after parsing the input query.
    /// </summary>
    public interface IParseResult
    {
        /// <summary>
        /// Returns <see langword="true"/> if no errors were found during the parse of a query.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Returns errors if query parsing fails.
        /// </summary>
        ExecutionErrors Errors { get; }

        /// <summary>
        /// Parsed <see cref="Document"/> if parsing is successful and null otherwise.
        /// </summary>
        Document ParsedDocument {get;}
    }
}