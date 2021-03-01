using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Guid scalar graph type represents a 128-bit globally unique identifier (GUID).
    /// </summary>
    public class GuidGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is StringValue s && Guid.TryParse(s.Value, out _);

        /// <inheritdoc/>
        public override bool CanParseValue(object value) => value is Guid || value is string s && Guid.TryParse(s, out _);

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            StringValue s => Guid.Parse(s.Value),
            NullValue _ => null,
            _ => ThrowLiteralConversionError(value)
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            Guid _ => value, // no boxing
            string s => Guid.Parse(s),
            _ => ThrowValueConversionError(value)
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => value switch
        {
            Guid g => new StringValue(g.ToString()),
            null => new NullValue(),
            _ => ThrowASTConversionError(value)
        };
    }
}
