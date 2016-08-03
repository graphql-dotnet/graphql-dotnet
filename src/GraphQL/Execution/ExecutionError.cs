using System;
using System.Collections.Generic;
using GraphQL.Language;
using GraphQL.Language.AST;

namespace GraphQL
{
    public class ExecutionError : Exception
    {
        private List<ErrorLocation> _errorLocations;

        public ExecutionError(string message)
            : base(message)
        {
        }

        public ExecutionError(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public IEnumerable<ErrorLocation> Locations => _errorLocations;

        public void AddLocation(int line, int column)
        {
            if (_errorLocations == null)
            {
                _errorLocations = new List<ErrorLocation>();
            }

            _errorLocations.Add(new ErrorLocation {Line = line, Column = column});
        }
    }

    public class ErrorLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public static class ExecutionErrorExtensions
    {
        public static void AddLocation(this ExecutionError error, Field field)
        {
            if (field != null)
            {
                error.AddLocation(field.SourceLocation.Line, field.SourceLocation.Column);
            }
        }
    }
}
