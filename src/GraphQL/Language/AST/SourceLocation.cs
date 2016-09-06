namespace GraphQL.Language.AST
{
    public class SourceLocation
    {
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

        public override string ToString()
        {
            return $"line={Line}, column={Column}, start={Start}, end={End}";
        }

        protected bool Equals(SourceLocation other)
        {
            return Line == other.Line && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SourceLocation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Line*397) ^ Column;
            }
        }
    }
}
