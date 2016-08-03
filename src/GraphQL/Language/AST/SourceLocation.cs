namespace GraphQL.Language.AST
{
    public class SourceLocation
    {
        public SourceLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public int Line { get; private set; }
        public int Column { get; private set; }

        public override string ToString()
        {
            return "line={0}, column={1}".ToFormat(Line, Column);
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
