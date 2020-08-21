using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL
{
    [Serializable]
    public class ExecutionError : Exception
    {
        private static readonly ConcurrentDictionary<Type, string> _exceptionErrorCodes = new ConcurrentDictionary<Type, string>();

        private List<ErrorLocation> _errorLocations;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class with a specified error message.
        /// </summary>
        public ExecutionError(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class with a specified error message and exception data.
        /// </summary>
        public ExecutionError(string message, IDictionary data)
            : base(message)
        {
            SetData(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionError"/> class with a specified error message. Sets the
        /// <see cref="Code"/> and <see cref="Codes"/> properties based on the inner exception(s). Loads any exception data
        /// from the inner exception into this instance.
        /// </summary>
        public ExecutionError(string message, Exception exception)
            : base(message, exception)
        {
            SetCode(exception);
            SetData(exception);
        }

        /// <summary>
        /// Returns a list of locations within the document that this error applies to.
        /// </summary>
        public IEnumerable<ErrorLocation> Locations => _errorLocations;

        /// <summary>
        /// Gets or sets a code for this error.
        /// </summary>
        public virtual string Code { get; set; }

        /// <summary>
        /// Returns true if there are any codes for this error.
        /// </summary>
        public virtual bool HasCodes => InnerException != null || !string.IsNullOrWhiteSpace(Code);

        /// <summary>
        /// Returns a list of codes for this error.
        /// </summary>
        public virtual IEnumerable<string> Codes
        {
            get
            {
                // Code could be set explicitly, and not through the constructor with the exception
                if (!string.IsNullOrWhiteSpace(Code) && (InnerException == null || Code != GetErrorCode(InnerException)))
                    yield return Code;

                var current = InnerException;

                while (current != null)
                {
                    yield return GetErrorCode(current);
                    current = current.InnerException;
                }
            }
        }

        /// <summary>
        /// Gets or sets the path within the GraphQL document where this error applies to.
        /// </summary>
        public IEnumerable<object> Path { get; set; }

        /// <summary>
        /// Adds a location to the list of locations that this error applies to.
        /// </summary>
        public void AddLocation(int line, int column)
        {
            if (_errorLocations == null)
            {
                _errorLocations = new List<ErrorLocation>();
            }

            _errorLocations.Add(new ErrorLocation { Line = line, Column = column });
        }

        private void SetCode(Exception exception)
        {
            if (exception != null)
                Code = GetErrorCode(exception);
        }

        private void SetData(Exception exception)
        {
            if (exception?.Data != null)
                SetData(exception.Data);
        }

        private void SetData(IDictionary dict)
        {
            if (dict != null)
            {
                foreach (DictionaryEntry keyValuePair in dict)
                {
                    Data[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        /// <summary>
        /// Generates an normalized error code for the specified exception by taking the type name, removing the "GraphQL" prefix, if any,
        /// removing the "Exception" suffix, if any, and then converting the result from PascalCase to UPPER_CASE.
        /// </summary>
        protected static string GetErrorCode(Exception exception) => _exceptionErrorCodes.GetOrAdd(exception.GetType(), NormalizeErrorCode);

        private static string NormalizeErrorCode(Type exceptionType)
        {
            var code = exceptionType.Name;

            if (code.EndsWith(nameof(Exception), StringComparison.InvariantCulture))
            {
                code = code.Substring(0, code.Length - nameof(Exception).Length);
            }

            if (code.StartsWith("GraphQL", StringComparison.InvariantCulture))
            {
                code = code.Substring("GraphQL".Length);
            }

            return GetAllCapsRepresentation(code);
        }

        private static string GetAllCapsRepresentation(string str)
        {
            return Regex
                .Replace(NormalizeString(str), @"([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
                .ToUpperInvariant();
        }

        private static string NormalizeString(string str)
        {
            str = str?.Trim();
            return string.IsNullOrWhiteSpace(str)
                ? string.Empty
                : NormalizeTypeName(str);
        }

        private static string NormalizeTypeName(string name)
        {
            var tickIndex = name.IndexOf('`');
            return tickIndex >= 0
                ? name.Substring(0, tickIndex)
                : name;
        }
    }

    public struct ErrorLocation : IEquatable<ErrorLocation>
    {
        public int Line { get; set; }

        public int Column { get; set; }

        public bool Equals(ErrorLocation other) => Line == other.Line && Column == other.Column;

        public override bool Equals(object obj) => obj is Location loc && Equals(loc);

        public override int GetHashCode() => (Line, Column).GetHashCode();

        public static bool operator ==(ErrorLocation left, ErrorLocation right) => left.Equals(right);

        public static bool operator !=(ErrorLocation left, ErrorLocation right) => !(left == right);
    }

    public static class ExecutionErrorExtensions
    {
        /// <summary>
        /// Adds a location to an <see cref="ExecutionError"/> based on a <see cref="AbstractNode"/> within a <see cref="Document"/>.
        /// </summary>
        public static void AddLocation(this ExecutionError error, AbstractNode abstractNode, Document document)
        {
            if (abstractNode == null)
                return;

            if (document != null)
            {
                var location = new Location(new Source(document.OriginalQuery), abstractNode.SourceLocation.Start);
                error.AddLocation(location.Line, location.Column);
            }
            else if (abstractNode.SourceLocation.Line > 0 && abstractNode.SourceLocation.Column > 0)
            {
                error.AddLocation(abstractNode.SourceLocation.Line, abstractNode.SourceLocation.Column);
            }
        }
    }
}
