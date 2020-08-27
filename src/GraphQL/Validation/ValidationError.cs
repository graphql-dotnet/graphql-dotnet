using System;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL.Validation
{
    /// <summary>
    /// Represents an error generated while validating the document.
    /// </summary>
    [Serializable]
    public class ValidationError : DocumentError
    {
        private readonly List<INode> _nodes = new List<INode>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified error message and code.
        /// Sets locations based on the original query and specified AST nodes that this error applies to.
        /// </summary>
        public ValidationError(string originalQuery, string number, string message, params INode[] nodes)
            : this(originalQuery, number, message, null, nodes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified error message and code.
        /// Sets locations based on the original query and specified AST nodes that this error applies to. Sets additional
        /// codes based on the inner exception(s). Loads any exception data from the inner exception into this instance.
        /// </summary>
        public ValidationError(
            string originalQuery,
            string number,
            string message,
            Exception innerException,
            params INode[] nodes)
            : base(message, innerException)
        {
            Code = GetValidationErrorCode(GetType());
            Number = number;

            nodes?.Apply(n =>
            {
                _nodes.Add(n);
                if (n.SourceLocation != null)
                {
                    var location = new Location(new Source(originalQuery), n.SourceLocation.Start);
                    AddLocation(location.Line, location.Column);
                }
            });
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
        public IEnumerable<INode> Nodes => _nodes;

        /// <summary>
        /// Gets or sets the rule number of this validation error corresponding to the paragraph number from the official specification.
        /// </summary>
        public string Number { get; set; }
    }
}
