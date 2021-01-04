namespace GraphQL.Language.AST
{
    /// <summary>
    /// Provides information regarding the location of a node within a document's original query text.
    /// </summary>
    public readonly struct SourceLocation
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        public SourceLocation(int start = -1, int end = -1)
        {
            _start = start + 1;
            _end = end + 1;
        }

        private readonly int _start;
        /// <summary>
        /// Returns the start position within the query text.
        /// </summary>
        public readonly int Start => _start - 1;

        private readonly int _end;
        /// <summary>
        /// Returns the end position within the query text.
        /// </summary>
        public int End => _end - 1;

        /// <summary>
        /// Indicates whether the structure has been initialized with a valid values.
        /// </summary>
        public bool IsSet => _start >= 0 && _end >= _start;

        /// <inheritdoc/>
        public override string ToString() => $"start={Start}, end={End}";
    }
}
