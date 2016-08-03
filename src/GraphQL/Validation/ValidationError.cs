using System;
using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Language.AST;

namespace GraphQL.Validation
{
    public class ValidationError : ExecutionError
    {
        private readonly List<INode> _nodes = new List<INode>();

        public ValidationError(string errorCode, string message, params INode[] nodes)
            : this(errorCode, message, null, nodes)
        {
        }

        public ValidationError(
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
                    AddLocation(n.SourceLocation.Line, n.SourceLocation.Column);
                }
            });
        }

        public string ErrorCode { get; }

        public IEnumerable<INode> Nodes => _nodes;
    }
}
