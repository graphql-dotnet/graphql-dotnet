using System;

namespace GraphQL.Language.AST
{
    public readonly struct SourceLocation : IEquatable<SourceLocation>
    {
        public static readonly SourceLocation Empty = new SourceLocation(-1, -1, -1, -1);

        public SourceLocation(int line, int column, int start = -1, int end = -1)
        {
            Line = line;
            Column = column;
            Start = start;
            End = end;
        }

        public int Start { get; }

        public int End { get; }

        public int Line { get; }

        public int Column { get; }

        /// <inheritdoc />
        public override string ToString() => $"line={Line}, column={Column}, start={Start}, end={End}";

        public bool Equals(SourceLocation other) => Line == other.Line && Column == other.Column; // TODO: where are start and end?

        public static bool operator ==(SourceLocation first, SourceLocation second) => first.Equals(second);

        public static bool operator !=(SourceLocation first, SourceLocation second) => !first.Equals(second);

        public override bool Equals(object obj) => obj is SourceLocation loc && Equals(loc);

        public override int GetHashCode() => (Line, Column).GetHashCode();
    }
}
