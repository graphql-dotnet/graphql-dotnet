using System;

namespace GraphQL.Language.AST
{
    public class StringValue : ValueNode<string>
    {
        public StringValue(string value) => Value = value;

        protected override bool Equals(ValueNode<string> other) => string.Equals(Value, other.Value, StringComparison.InvariantCulture);
    }
}
