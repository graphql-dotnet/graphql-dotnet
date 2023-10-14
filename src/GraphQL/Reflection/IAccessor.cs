using System.Reflection;

namespace GraphQL.Reflection
{
    /// <summary>
    /// An abstraction around accessing a property or method on a object instance.
    /// </summary>
    public interface IAccessor
    {
        /// <summary>
        /// Returns the name of the member that this accessor points to.
        /// </summary>
        string FieldName { get; }

        /// <summary>
        /// Returns the data type that the member returns.
        /// </summary>
        Type ReturnType { get; }

        /// <summary>
        /// Returns the type where the member is defined.
        /// </summary>
        Type DeclaringType { get; }

        /// <summary>
        /// For methods, returns a list of parameters defined for the method, otherwise <see langword="null"/>.
        /// </summary>
        ParameterInfo[]? Parameters { get; }

        /// <summary>
        /// Returns a <see cref="MethodInfo"/> instance that points to the member.
        /// For properties, this points to the property getter.
        /// </summary>
        MethodInfo MethodInfo { get; }

        /// <summary>
        /// Get return value of method or property.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <param name="arguments">Arguments for method; not used for property.</param>
        /// <returns>Return value.</returns>
        object? GetValue(object target, object?[]? arguments);

        /// <summary>
        /// Returns a list of attributes of the specified type defined on the member.
        /// </summary>
        /// <typeparam name="T">The type of the attribute.</typeparam>
        IEnumerable<T> GetAttributes<T>() where T : Attribute;
    }
}
