using System;
using GraphQL.Language;
using GraphQL.Types;

namespace GraphQl.SchemaGenerator.Wrappers
{
    /// <summary>
    ///     Byte array graph type.
    /// </summary>
    public class ByteArrayGraphType : ScalarGraphType
    {
        #region Constructor

        /// <summary>
        ///     Constructor.
        /// </summary>
        public ByteArrayGraphType()
        {
            Name = "Base64";
            Description = "Byte array";
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Parse value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ParseValue(object value)
        {
            var bytes = value as byte[];
            if (bytes != null)
            {
                return Convert.ToBase64String(bytes);
            }

            return null;
        }

        /// <summary>
        ///     Serialize.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object Serialize(object value)
        {
            return ParseValue(value);
        }

        /// <summary>
        ///     Parse literal.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object ParseLiteral(IValue value)
        {
            var val = value as StringValue;

            if (val == null)
            {
                return null;
            }

            return ParseValue(val.Value);
        }

        #endregion
    }
}
