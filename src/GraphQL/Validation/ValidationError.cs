using GraphQL.Execution;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents an error generated while validating the document.
    /// </summary>
    [Serializable]
    public class ValidationError : DocumentError
    {
        private readonly List<ASTNode> _nodes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified error message.
        /// Sets the <see cref="ExecutionError.Code">Code</see> property based on the exception type.
        /// </summary>
        public ValidationError(string message) : base(message)
        {
            Code = GetValidationErrorCode(GetType());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified
        /// error message and inner exception. Sets the <see cref="ExecutionError.Code">Code</see>
        /// property based on the exception type. Loads any exception data from the inner exception
        /// into this instance.
        /// </summary>
        public ValidationError(string message, Exception? innerException) : base(message, innerException)
        {
            Code = GetValidationErrorCode(GetType());
        }

        /// <inheritdoc cref="ValidationError(ROM, string, string, ASTNode[])"/>
        public ValidationError(ROM originalQuery, string number, string message, ASTNode node)
            : this(originalQuery, number, message, (Exception?)null, node)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified
        /// error message, code and number. Sets locations based on the original query and specified
        /// AST nodes that this error applies to.
        /// </summary>
        public ValidationError(ROM originalQuery, string number, string message, params ASTNode[] nodes)
            : this(originalQuery, number, message, null, nodes)
        {
        }

        /// <inheritdoc cref="ValidationError(ROM, string, string, Exception, ASTNode[])"/>
        public ValidationError(
            ROM originalQuery,
            string number,
            string message,
            Exception? innerException,
            ASTNode node)
            : base(message, innerException)
        {
            Code = GetValidationErrorCode(GetType());
            Number = number;

            if (node != null)
            {
                _nodes.Add(node);
                AddLocation(Location.FromLinearPosition(originalQuery, node.Location.Start));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified
        /// error message and inner exception. Sets the <see cref="ExecutionError.Code">Code</see>
        /// property based on the exception type. Sets locations based on the original query and
        /// specified AST nodes that this error applies to. Loads any exception data from the inner
        /// exception into this instance.
        /// </summary>
        public ValidationError(
            ROM originalQuery,
            string number,
            string message,
            Exception? innerException,
            params ASTNode[]? nodes)
            : base(message, innerException)
        {
            Code = GetValidationErrorCode(GetType());
            Number = number;

            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    _nodes.Add(n);
                    AddLocation(Location.FromLinearPosition(originalQuery, n.Location.Start));
                }
            }
        }

        internal static string GetValidationErrorCode(Type type)
        {
            var code = ErrorInfoProvider.GetErrorCode(type);
            if (code != "VALIDATION_ERROR" && code.EndsWith("_ERROR"))
                code = code.Substring(0, code.Length - 6);
            return code;
        }

        /// <summary>
        /// Returns a list of AST nodes that this error applies to.
        /// </summary>
        public IEnumerable<ASTNode> Nodes => _nodes;

        /// <summary>
        /// Gets or sets the rule number of this validation error corresponding
        /// to the paragraph number from the official specification if any.
        /// </summary>
        public string? Number { get; set; }
    }
}
