using GraphQL.Language.AST;

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
        /// </summary>
        /// <param name="value"> Resolved value. </param>
        /// <returns>
        /// The returned value of a the result coercion is part of the overall execution result.
        /// Normally this value is a primitive value like String or Integer to make it easy for
        /// the serialization layer. For complex types like a Date or Money Scalar this involves
        /// formatting the value. Returning <see langword="null"/> indicates a failed conversion.
        /// </returns>
        public virtual object Serialize(object value) => ParseValue(value);

        /// <summary>
        /// Literal input coercion. It takes an abstract syntax tree (AST) element from a schema
        /// definition or query and converts it into an appropriate internal value. In other words
        /// it transforms a scalar from its client-side representation as an argument to its
        /// server-side representation. Input coercion may not only return primitive values like
        /// String but rather complex ones when appropriate.
        /// </summary>
        /// <param name="value"> AST value node. </param>
        /// <returns> Internal scalar representation. Returning <see langword="null"/> indicates a failed conversion. </returns>
        public abstract object ParseLiteral(IValue value);

        /// <summary>
        /// Value input coercion. Argument values can not only provided via GraphQL syntax inside a
        /// query, but also via variable. It transforms a scalar from its client-side representation
        /// as a variable to its server-side representation.
        /// <br/><br/>
        /// Parsing for arguments and variables are handled separately because while arguments must
        /// always be expressed in GraphQL query syntax, variable format is transport-specific (usually JSON).
        /// </summary>
        /// <param name="value"> Runtime object from variables. </param>
        /// <returns> Internal scalar representation. Returning <see langword="null"/> indicates a failed conversion. </returns>
        public abstract object ParseValue(object value);

        /// <summary>
        /// Checks for literal input coercion possibility. It takes an abstract syntax tree (AST) element from a schema
        /// definition or query and checks if it can be converted into an appropriate internal value. In other words
        /// it checks if a scalar can be converted from its client-side representation as an argument to its
        /// server-side representation.
        /// <br/>
        /// This method can be overridden to validate input values without directly getting those values, i.e. without boxing.
        /// </summary>
        /// <param name="value"> AST value node. </param>
        public virtual bool CanParseLiteral(IValue value)
        {
            try
            {
                return ParseLiteral(value) != null;
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
        /// <br/>
        /// This method can be overridden to validate input values without directly getting those values, i.e. without boxing.
        /// </summary>
        /// <param name="value"> Runtime object from variables. </param>
        public virtual bool CanParseValue(object value)
        {
            try
            {
                return ParseValue(value) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
