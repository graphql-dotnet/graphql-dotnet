using System;

namespace GraphQL.Language.AST
{
    public class DateTimeValue : ValueNode<DateTime>
    {
        public DateTimeValue(DateTime value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<DateTime> other)
        {
            return DateTime.Equals(Value, other.Value);
        }
    }
}