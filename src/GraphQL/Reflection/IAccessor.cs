using System;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQL.Reflection
{
    /// <summary>
    /// An abstraction around accessing a property or method on a object instance
    /// </summary>
    public interface IAccessor
    {
        string FieldName { get; }

        Type ReturnType { get; }

        Type DeclaringType { get; }

        ParameterInfo[] Parameters { get; }

        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Get return value of method or property.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <param name="arguments">Arguments for method; not used for property.</param>
        /// <returns>Return value.</returns>
        object GetValue(object target, object[] arguments);

        IEnumerable<T> GetAttributes<T>() where T : Attribute;
    }
}
