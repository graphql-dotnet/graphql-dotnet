using System;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQL.Reflection
{
    internal class SinglePropertyAccessor : IAccessor
    {
        private PropertyInfo _getter;

        public SinglePropertyAccessor(PropertyInfo getter)
        {
            _getter = getter;
        }

        public string FieldName => _getter.Name;
        public Type ReturnType => _getter.PropertyType;
        public Type DeclaringType => _getter.DeclaringType;
        public ParameterInfo[] Parameters => _getter.GetMethod.GetParameters();
        public MethodInfo MethodInfo => _getter.GetMethod;
        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return _getter.GetCustomAttributes<T>();
        }

        public object GetValue(object target, object[] arguments)
        {
            return _getter.GetValue(target, null);
        }
    }
}
