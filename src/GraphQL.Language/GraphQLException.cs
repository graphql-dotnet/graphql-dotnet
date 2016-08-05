using System;

namespace GraphQL.Language
{
    public class GraphQLException : Exception
    {
        public GraphQLException(string message) : base(message)
        {
        }

        public GraphQLException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
