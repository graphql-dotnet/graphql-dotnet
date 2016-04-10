using System;
using GraphQL.Language;

namespace GraphQL
{
    public class ExecutionError : Exception
    {
        public Field Field { get; set; }

        public ExecutionError(Field field, string message)
            : this(field, message, null)
        {
        }

        public ExecutionError(Field field, string message, Exception innerException)
            : base(message, innerException)
        {
            Field = field;
        }
    }
}
