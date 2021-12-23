using System;
using System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}

#endif

namespace GraphQL
{
    internal static class NotNullExtensions
    {
        public static T NotNull<T>(this T value, [CallerArgumentExpression("value")] string name = "")
            where T: class
        {
            return value ?? throw new ArgumentNullException(name);
        }
    }
}
