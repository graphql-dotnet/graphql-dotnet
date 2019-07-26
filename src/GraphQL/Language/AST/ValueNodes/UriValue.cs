using System;

namespace GraphQL.Language.AST
{
    public class UriValue : ValueNode<Uri>
    {
        public UriValue(Uri value) => Value = value;

        protected override bool Equals(ValueNode<Uri> other) => Equals(Value, other.Value);
    }
}
