using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Reflection
{
    internal class SinglePropertyAccessor : IAccessor
    {
        private readonly PropertyInfo _property;

        public SinglePropertyAccessor(PropertyInfo property)
        {
            _property = property;
        }

        public string FieldName => _property.Name;

        public Type ReturnType => _property.PropertyType;

        public Type DeclaringType => _property.DeclaringType!;

        public ParameterInfo[]? Parameters => null;

        public MethodInfo MethodInfo => _property.GetMethod!;

        public IEnumerable<T> GetAttributes<T>() where T : Attribute => _property.GetCustomAttributes<T>();

        public object? GetValue(object target, object?[]? arguments)
        {
            try
            {
                return _property.GetValue(target, null);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return null; // never executed, necessary only for intellisense
            }
        }
    }
}
