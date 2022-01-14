using System;
using GraphQLParser.AST;

namespace GraphQL.Language.AST
{
    /// <summary>
    /// Represents a <see cref="double"/> value within a document.
    /// </summary>
    public class FloatValue : GraphQLFloatValue, IValue<double>
    {
        /// <summary>
        /// Initializes a new instance with the specified value.
        /// </summary>
        public FloatValue(double value)
        {
            ClrValue = ValidateValue(value);
        }

        public double ClrValue { get; }

        object? IValue.ClrValue => ClrValue;

        private static double ValidateValue(double value)
        {
            // TODO: see https://github.com/graphql-dotnet/graphql-dotnet/pull/2379#issuecomment-800828568 and https://github.com/graphql-dotnet/graphql-dotnet/pull/2379#issuecomment-800906086
            // if (double.IsNaN(value) || double.IsInfinity(value))
            if (double.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be NaN."); // Value cannot be NaN or Infinity.

            return value;
        }
    }
}
