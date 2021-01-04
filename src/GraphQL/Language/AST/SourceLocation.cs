using System;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Provides information regarding the location of a node within a document's original query text.
    /// </summary>
    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        public static readonly SourceLocation Empty = new SourceLocation(-1, -1, -1, -1);

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        public SourceLocation(int line, int column, int start = -1, int end = -1)
        {
            Line = line;
            Column = column;
            Start = start;
            End = end;
        }

        /// <summary>
        /// Returns the start position within the query text.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Returns the end position within the query text.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Returns the line number within the query text.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Returns the column number within the query text.
        /// </summary>
        public int Column { get; }

        /// <inheritdoc/>
        public override string ToString() => $"line={Line}, column={Column}, start={Start}, end={End}";

        /// <summary>
        /// Compares this instance to another based on the line and column values.
        /// </summary>
        public bool Equals(SourceLocation other) => Line == other.Line && Column == other.Column; // TODO: where are start and end?

        public static bool operator ==(SourceLocation first, SourceLocation second) => first.Equals(second);

        public static bool operator !=(SourceLocation first, SourceLocation second) => !first.Equals(second);

        public override bool Equals(object obj) => obj is SourceLocation loc && Equals(loc);

        /// <inheritdoc/>
        public override int GetHashCode() => (Line, Column).GetHashCode();
    }
}
