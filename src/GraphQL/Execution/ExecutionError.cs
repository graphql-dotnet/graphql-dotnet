using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQLParser;

namespace GraphQL
{
    /// <summary>
    /// Represents an error generated while processing a document and intended to be returned within an <see cref="ExecutionResult"/>.
    /// </summary>
    [Serializable]
    public class ExecutionError : Exception
    {
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
        /// <see cref="Code"/> property based on the inner exception. Loads any exception data
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
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the path within the GraphQL document where this error applies to.
        /// </summary>
        public IEnumerable<object> Path { get; set; }

        /// <summary>
        /// Adds a location to the list of locations that this error applies to.
        /// </summary>
        public void AddLocation(int line, int column)
        {
            _errorLocations ??= new List<ErrorLocation>();

            _errorLocations.Add(new ErrorLocation { Line = line, Column = column });
        }

        private void SetCode(Exception exception)
        {
            if (exception != null)
                Code = ErrorInfoProvider.GetErrorCode(exception);
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
    }

    /// <summary>
    /// Represents a location within a document where a parsing or execution error occurred.
    /// </summary>
    public struct ErrorLocation : IEquatable<ErrorLocation>
    {
        /// <summary>
        /// The line number of the document where the error occurred, where 1 is the first line.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The column number of the document where the error occurred, where 1 is the first column.
        /// </summary>
        public int Column { get; set; }

        /// <inheritdoc/>
        public bool Equals(ErrorLocation other) => Line == other.Line && Column == other.Column;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Location loc && Equals(loc);

        /// <inheritdoc/>
        public override int GetHashCode() => (Line, Column).GetHashCode();

        /// <summary>
        /// Indicates whether two <see cref="ErrorLocation"/> instances are the same.
        /// </summary>
        public static bool operator ==(ErrorLocation left, ErrorLocation right) => left.Equals(right);

        /// <summary>
        /// Indicates whether two <see cref="ErrorLocation"/> instances are not the same.
        /// </summary>
        public static bool operator !=(ErrorLocation left, ErrorLocation right) => !(left == right);
    }

    /// <summary>
    /// Provides extension methods for <see cref="ExecutionError"/> instances.
    /// </summary>
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
