using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional value. It is one of the five built-in scalars.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="int"/> .NET values to this scalar graph type.
    /// </summary>
    public class IntGraphType : ScalarGraphType
    {
        /// <inheritdoc/>
        public override object ParseLiteral(IValue value)
        {
            if (value is IntValue intValue)
            {
                return intValue.Value;
            }

            return null;
        }

        /// <inheritdoc/>
        public override object ParseValue(object value) => ValueConverter.ConvertTo(value, typeof(int));

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value is IntValue;

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new IntValue(Convert.ToInt32(value));
    }
}
