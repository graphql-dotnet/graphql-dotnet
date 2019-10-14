using System;

namespace GraphQL.Language.AST
{
    public class TimeSpanValue : ValueNode<TimeSpan>
    {
        public TimeSpanValue(TimeSpan value) => Value = value;

        protected override bool Equals(ValueNode<TimeSpan> other) => TimeSpan.Equals(Value, other.Value);
    }
}
