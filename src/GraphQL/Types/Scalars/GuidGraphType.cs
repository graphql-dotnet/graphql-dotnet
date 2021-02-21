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
        protected override bool CanParseLiteralCore(IValue value) => value switch
        {
            GuidValue _ => true,
            StringValue s => Guid.TryParse(s.Value, out _),
            _ => false
        };

        /// <inheritdoc/>
        protected override bool CanParseValueCore(object value) => value switch
        {
            Guid _ => true,
            string s => Guid.TryParse(s, out _),
            _ => false
        };

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            GuidValue g => g.Value,
            StringValue s => Guid.Parse(s.Value),
            _ => null
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value switch
        {
            Guid _ => value, // no boxing
            string s => Guid.Parse(s),
            _ => null
        };
    }
}
