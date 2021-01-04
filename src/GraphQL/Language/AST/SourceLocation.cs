namespace GraphQL.Language.AST
{
    /// <summary>
    /// Provides information regarding the location of a node within a document's original query text.
    /// </summary>
    public class SourceLocation
    {
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
        protected bool Equals(SourceLocation other)
        {
            return Line == other.Line && Column == other.Column;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((SourceLocation)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => (Line, Column).GetHashCode();
    }
}
