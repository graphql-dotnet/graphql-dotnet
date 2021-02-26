using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Short scalar graph type represents a signed 16-bit integer value.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="short"/> .NET values to this scalar graph type.
    /// </summary>
    public class ShortGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value)
            => value is IntValue intValue && short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue ? (short?)intValue.Value : null;

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(short));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value)
            => value is IntValue intValue && short.MinValue <= intValue.Value && intValue.Value <= short.MaxValue;

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToInt16(value));
    }
}
