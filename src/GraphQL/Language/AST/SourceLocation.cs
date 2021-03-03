using System;
using System.Diagnostics;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Provides information regarding the location of a node within a document's original query text.
    /// </summary>
    [DebuggerDisplay("(Start={Start}, End={End})")]
    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        public SourceLocation(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// The index for the start of the node in the source (i.e. it's inclusive).
        /// <br/>
        /// For example:
        /// <code>
        /// { field { subfield } }
        /// <br/>
        /// --^ field.Location.Start = 2
        /// </code>
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// The index for the character immediately after the node in the source (i.e. it's exclusive).
        /// <br/>
        /// For example:
        /// <code>
        /// { field { subfield } }
        /// <br/>
        /// --------------------^ field.Location.End = 20
        /// </code>
        /// </summary>
        public int End { get; }

        /// <inheritdoc/>
        public bool Equals(SourceLocation other) => Start == other.Start && End == other.End;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is SourceLocation l && Equals(l);

        /// <inheritdoc/>
        public override int GetHashCode() => (Start, End).GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"({Start},{End})";
    }
}
