using System;

namespace GraphQL.Language.AST
{
    public class DateTimeOffsetValue : ValueNode<DateTimeOffset>
    {
        public DateTimeOffsetValue(DateTimeOffset value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<DateTimeOffset> other)
        {
            return DateTimeOffset.Equals(Value, other.Value);
        }
    }
}