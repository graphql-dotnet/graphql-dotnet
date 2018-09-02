using System;

namespace GraphQL.Language.AST
{
    public class GuidValue : ValueNode<Guid>
    {
        public GuidValue(Guid value) => Value = value;

        protected override bool Equals(ValueNode<Guid> other) => Value.Equals(other.Value);
    }
}
