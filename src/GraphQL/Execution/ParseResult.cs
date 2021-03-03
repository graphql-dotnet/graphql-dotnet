using GraphQL.Language.AST;

namespace GraphQL.Execution
{
    /// <inheritdoc cref="IParseResult"/>
    public class ParseResult : IParseResult
    {
        public ParseResult(SyntaxError error)
        {
            Errors.Add(error);
        }
        /// <summary>
        /// returns true indicating parsing failure
        /// </summary>
        public bool IsValid => false;
        /// <summary>
        /// returns error(s) occurred during parsing
        /// </summary>
        public ExecutionErrors Errors { get; } = new ExecutionErrors();
        /// <summary>
        /// Accessing this property will result in exception since it is populated only if the parsing is successful.
        /// </summary>
        public Document ParsedDocument => throw new InvalidOperationError("ParsedDocument is accessible only when IsValid=true");
    }

    /// <summary>
    /// A parse result which indicates that there was no parse error.
    /// </summary>
    public class SuccessfullyParsedResult : IParseResult
    {
        public SuccessfullyParsedResult(Document document)
        {
            ParsedDocument = document;
        }
        /// <summary>
        /// returns true indicating successful parsing.
        /// </summary>
        public bool IsValid => true;
        /// <summary>
        /// Accessing this property will result in exception since it's populated only if the parsing fails.
        /// </summary>
        public ExecutionErrors Errors => throw new InvalidOperationError("Error is accessible only when IsValid=false");
        /// <summary>
        /// returns a <see cref="Document"> instance representing the input query.
        /// </summary>
        public Document ParsedDocument { get; }
    }
}