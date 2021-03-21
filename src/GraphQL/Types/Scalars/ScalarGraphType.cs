using System;
using System.Numerics;
using GraphQL.Language.AST;

#nullable enable

namespace GraphQL.Types
{
    /// <summary>
    /// Scalar types represent the leaves of the query - those fields that don't have any sub-fields.
    /// <br/><br/>
    /// <see href="https://github.com/graphql-dotnet/graphql-dotnet/blob/master/docs2/site/docs/getting-started/custom-scalars.md">More info</see> about scalars.
    /// </summary>
    public abstract class ScalarGraphType : GraphType
    {
        /// <summary>
        /// Result (output) coercion. It takes the result of a resolver and converts it into an
        /// appropriate value for the output result. In other words it transforms a scalar from
        /// its server-side representation to a representation suitable for the client.
        /// <br/><br/>
        /// Since GraphQL specifies no response format, Serialize is not
        /// responsible for preparing the scalar for transport to the client. It is only responsible
        /// for generating an object which can eventually be serialized by some transport-focused API.
        /// <br/><br/>
        /// This method should handle a value of <see langword="null"/>, but may throw an exception
        /// if <see langword="null"/> is an invalid internal scalar representation.
        /// </summary>
        /// <param name="value">Resolved value (internal scalar representation). May be <see langword="null"/>.</param>
        /// <returns>
        /// The returned value of a the result coercion is part of the overall execution result.
        /// Normally this value is a primitive value like String or Integer to make it easy for
        /// the serialization layer. For complex types like a Date or Money scalar this involves
        /// formatting the value. Returning <see langword="null"/> is valid.
        /// </returns>
        public virtual object? Serialize(object? value) => ParseValue(value);

        /// <summary>
        /// Literal input coercion. It takes an abstract syntax tree (AST) element from a schema
        /// definition or query and converts it into an appropriate internal value. In other words
        /// it transforms a scalar from its client-side representation as an argument to its
        /// server-side representation. Input coercion may not only return primitive values like
        /// String but rather complex ones when appropriate.
        /// <br/><br/>
        /// This method must handle a value of <see cref="NullValue"/>.
        /// </summary>
        /// <param name="value">AST value node. Must not be <see langword="null"/>, but may be <see cref="NullValue"/>.</param>
        /// <returns>Internal scalar representation. Returning <see langword="null"/> is valid.</returns>
        public virtual object? ParseLiteral(IValue value) => value switch
        {
            BooleanValue b => ParseValue(b.Value.Boxed()),
            IntValue i => ParseValue(i.Value),
            LongValue l => ParseValue(l.Value),
            BigIntValue bi => ParseValue(bi.Value),
            FloatValue f => ParseValue(f.Value),
            DecimalValue d => ParseValue(d.Value),
            StringValue s => ParseValue(s.Value),
            NullValue _ => ParseValue(null),
            _ => ThrowLiteralConversionError(value)
        };

        /// <summary>
        /// Value input coercion. Argument values can not only provided via GraphQL syntax inside a
        /// query, but also via variable. It transforms a scalar from its client-side representation
        /// as a variable to its server-side representation.
        /// <br/><br/>
        /// Parsing for arguments and variables are handled separately because while arguments must
        /// always be expressed in GraphQL query syntax, variable format is transport-specific (usually JSON).
        /// <br/><br/>
        /// This method must handle a value of <see langword="null"/>.
        /// </summary>
        /// <param name="value">Runtime object from variables. May be <see langword="null"/>.</param>
        /// <returns>Internal scalar representation. Returning <see langword="null"/> is valid.</returns>
        public abstract object? ParseValue(object? value);

        /// <summary>
        /// Checks for literal input coercion possibility. It takes an abstract syntax tree (AST) element from a schema
        /// definition or query and checks if it can be converted into an appropriate internal value. In other words
        /// it checks if a scalar can be converted from its client-side representation as an argument to its
        /// server-side representation.
        /// <br/><br/>
        /// This method can be overridden to validate input values without directly getting those values, i.e. without boxing.
        /// <br/><br/>
        /// This method must return <see langword="true"/> when passed a <see cref="NullValue"/> node.
        /// </summary>
        /// <param name="value">AST value node. Must not be <see langword="null"/>, but may be <see cref="NullValue"/>.</param>
        public virtual bool CanParseLiteral(IValue value)
        {
            try
            {
                _ = ParseLiteral(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for value input coercion possibility. Argument values can not only provided via GraphQL syntax inside a
        /// query, but also via variable. It checks if a scalar can be converted from its client-side representation
        /// as a variable to its server-side representation.
        /// <br/><br/>
        /// Parsing for arguments and variables are handled separately because while arguments must
        /// always be expressed in GraphQL query syntax, variable format is transport-specific (usually JSON).
        /// <br/><br/>
        /// This method can be overridden to validate input values without directly getting those values, i.e. without boxing.
        /// <br/><br/>
        /// This method must return <see langword="true"/> when passed a <see langword="null"/> value.
        /// </summary>
        /// <param name="value">Runtime object from variables. May be <see langword="null"/>.</param>
        public virtual bool CanParseValue(object? value)
        {
            try
            {
                _ = ParseValue(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks that the provided value is a valid default value.
        /// This method should not throw an exception.
        /// </summary>
        /// <param name="value">The value to examine. Must not be <see langword="null"/>, as that indicates the lack of a default value.</param>
        public virtual bool IsValidDefault(object value)
        {
            if (value == null)
                return false;

            try
            {
                return ToAST(value) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a value to an AST representation. This is necessary for introspection queries
        /// to return the default values of this scalar type when used on input fields or field and directive arguments.
        /// This method may throw an exception or return <see langword="null"/> for a failed conversion.
        /// May return <see cref="NullValue"/>.
        /// </summary>
        /// <param name="value">The value to convert. May be <see langword="null"/>.</param>
        /// <returns>AST representation of the specified value. Returning <see langword="null"/> indicates a failed conversion. Returning <see cref="NullValue"/> is valid.</returns>
        public virtual IValue? ToAST(object? value)
        {
            var serialized = Serialize(value);
            return serialized switch
            {
                bool b => new BooleanValue(b),
                byte b => new IntValue(b),
                sbyte sb => new IntValue(sb),
                short s => new IntValue(s),
                ushort us => new IntValue(us),
                int i => new IntValue(i),
                uint ui => new LongValue(ui),
                long l => new LongValue(l),
                ulong ul => new BigIntValue(ul),
                BigInteger bi => new BigIntValue(bi),
                decimal d => new DecimalValue(d),
                float f => new FloatValue(f),
                double d => new FloatValue(d),
                string s => new StringValue(s),
                null => new NullValue(),
                _ => throw new NotImplementedException($"Please override the '{nameof(ToAST)}' method of the '{GetType().Name}' scalar to support this operation.")
            };
        }

        /// <summary>
        /// Throws an exception indicating that a value cannot be converted to its AST representation. Typically called by
        /// <see cref="ToAST(object)"/> if the provided object (an internal representation) is not valid for this scalar type.
        /// </summary>
        protected internal IValue ThrowASTConversionError(object? value)
        {
            throw new InvalidOperationException($"Unable to convert '{value ?? "(null)"}' to its AST representation for the scalar type '{Name}'.");
        }

        /// <summary>
        /// Throws an exception indicating that an AST scalar node cannot be converted to a value. Typically called by
        /// <see cref="ParseLiteral(IValue)"/> if the node type is invalid or cannot be parsed.
        /// </summary>
        protected object ThrowLiteralConversionError(IValue input)
        {
            throw new InvalidOperationException($"Unable to convert '{input}' literal from AST representation to the scalar type '{Name}'");
        }

        /// <summary>
        /// Throws an exception indicating that an external value (typically provided through a variable) cannot be converted
        /// to an internal representation. Typically called by <see cref="ParseValue(object)"/> if the provided object is invalid
        /// or cannot be parsed.
        /// </summary>
        /// <remarks>
        /// This is often called for serialization errors, since <see cref="Serialize(object)"/> calls <see cref="ParseValue(object)"/> by default.
        /// This also may be called for serialization errors during <see cref="ToAST(object)"/>, since <see cref="ToAST(object)"/> calls <see cref="Serialize(object)"/>
        /// by default, which by default calls <see cref="ParseValue(object)"/>.
        /// </remarks>
        protected object ThrowValueConversionError(object? value)
        {
            throw new InvalidOperationException($"Unable to convert '{value ?? "(null)"}' to the scalar type '{Name}'");
        }

        /// <summary>
        /// Throws an exception indicating that an internal value (typically returned from a field resolver) cannot be converted
        /// to its external representation. Typically called by <see cref="Serialize(object)"/> if the object is not valid for this
        /// scalar type.
        /// </summary>
        /// <remarks>
        /// This may be called for serialization errors during <see cref="ToAST(object)"/>, since the default implementation
        /// of <see cref="ToAST(object)"/> calls <see cref="Serialize(object)"/> to serialize the value before converting the
        /// result to an AST node.
        /// </remarks>
        protected object ThrowSerializationError(object? value)
        {
            throw new InvalidOperationException($"Unable to serialize '{value ?? "(null)"}' to the scalar type '{Name}'.");
        }
    }
}
