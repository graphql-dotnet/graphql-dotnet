using System;
using System.Collections.Generic;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL.Validation
{
    public class ValidationError : ExecutionError
    {
        private readonly List<INode> _nodes = new List<INode>();

        public ValidationError(string originalQuery, string errorCode, string message, params INode[] nodes)
            : this(originalQuery, errorCode, message, null, nodes)
        {
        }

        public ValidationError(
            string originalQuery,
            string errorCode,
            string message,
            Exception innerException,
            params INode[] nodes)
            : base(message, innerException)
        {
            ErrorCode = errorCode;

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

        public string ErrorCode { get; }

        public IEnumerable<INode> Nodes => _nodes;
    }
}
