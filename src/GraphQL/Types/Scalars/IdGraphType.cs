using System;
using GraphQL.Language.AST;

namespace GraphQL.Types
{
    /// <summary>
    /// The ID scalar graph type represents a string identifier, not intended to be human-readable. It is one of the five built-in scalars.
    /// When expected as an input type, any string or integer input value will be accepted as an ID.
    /// By default <see cref="SchemaTypes"/> maps all <see cref="Guid"/> .NET values to this scalar graph type.
    /// </summary>
    public class IdGraphType : ScalarGraphType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdGraphType"/> class.
        /// </summary>
        public IdGraphType()
        {
            Name = "ID";
            //Description =
            //    "The `ID` scalar type represents a unique identifier, often used to re-fetch an object or " +
            //    "as key for a cache. The `ID` type appears in a JSON response as a `String`; however, it " +
            //    "is not intended to be human-readable. When expected as an input type, any string (such " +
            //    "as `\"4\"`) or integer (such as `4`) input value will be accepted as an `ID`.";
        }

        /// <inheritdoc/>
        public override object Serialize(object value) => value.ToString();

        /// <inheritdoc/>
        public override object ParseLiteral(IValue value) => value switch
        {
            StringValue str => ParseValue(str.Value),
            IntValue num => num.Value,
            LongValue longVal => longVal.Value,
            _ => null,
        };

        /// <inheritdoc/>
        public override object ParseValue(object value) => value.ToString().Trim(' ', '"');

        /// <inheritdoc/>
        public override bool CanParseLiteral(IValue value) => value switch
        {
            StringValue _ => true,
            IntValue _ => true,
            LongValue _ => true,
            _ => false
        };

        /// <inheritdoc/>
        public override IValue ToAST(object value) => new StringValue(value.ToString());
    }
}
