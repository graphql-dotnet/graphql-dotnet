using System;
using System.Collections;
using System.Collections.Generic;
using GraphQL.Execution;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL
{
    /// <summary>
    /// Represents an error generated while processing a document and intended to be returned within an <see cref="ExecutionResult"/>.
    /// </summary>
    [Serializable]
    public class ExecutionError : Exception
    {
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
        public ExecutionError(string message, Exception? exception)
            : base(message, exception)
        {
            SetCode(exception);
            SetData(exception);
        }

        /// <summary>
        /// Returns a list of locations within the document that this error applies to.
        /// </summary>
        public List<ErrorLocation>? Locations { get; private set; }

        /// <summary>
        /// Gets or sets a code for this error.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the path within the GraphQL document where this error applies to.
        /// </summary>
        public IEnumerable<object>? Path { get; set; }

        /// <summary>
        /// Adds a location to the list of locations that this error applies to.
        /// </summary>
        public void AddLocation(int line, int column)
        {
            (Locations ??= new List<ErrorLocation>()).Add(new ErrorLocation(line, column));
        }

        private void SetCode(Exception? exception)
        {
            if (exception != null)
                Code = ErrorInfoProvider.GetErrorCode(exception);
        }

        private void SetData(Exception? exception)
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
    public readonly struct ErrorLocation : IEquatable<ErrorLocation>
    {
        /// <summary>
        /// Initializes a new instance with the specified line and column.
        /// </summary>
        public ErrorLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }

        /// <summary>
        /// The line number of the document where the error occurred, where 1 is the first line.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The column number of the document where the error occurred, where 1 is the first column.
        /// </summary>
        public int Column { get; }

        /// <inheritdoc/>
        public bool Equals(ErrorLocation other) => Line == other.Line && Column == other.Column;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Location loc && Equals(loc);

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
        /// Adds a location to an <see cref="ExecutionError"/> based on a <see cref="ASTNode"/> within a <see cref="GraphQLDocument"/>.
        /// </summary>
        public static TError AddLocation<TError>(this TError error, ASTNode? abstractNode, GraphQLDocument? document, string? originalQuery)
            where TError : ExecutionError
        {
            if (abstractNode == null || document == null || originalQuery == null || abstractNode.Location == default)
                return error;

            var location = new Location(originalQuery, abstractNode.Location.Start);
            error.AddLocation(location.Line, location.Column);
            return error;
        }
    }
}
