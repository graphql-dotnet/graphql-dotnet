using System;
using System.Reflection;

namespace GraphQL.Reflection
{
    /// <summary>
    /// Exception when nullable reference annotations cannot be read from a member.
    /// </summary>
    public class NullabilityInterpretationException : Exception
    {
        /// <summary>
        /// Initializes a new instance for the specified <paramref name="parameter"/>.
        /// </summary>
        public NullabilityInterpretationException(ParameterInfo parameter) : base($"Unable to interpret nullability attributes for parameter '{parameter.Name}' on method '{parameter.Member.DeclaringType.Name}.{parameter.Member.Name}'.")
        {
        }

        /// <summary>
        /// Initializes a new instance for the specified <paramref name="member"/>.
        /// </summary>
        public NullabilityInterpretationException(MemberInfo member) : base($"Unable to interpret nullability attributes for '{member.DeclaringType.Name}.{member.Name}'.")
        {
        }
    }
}
