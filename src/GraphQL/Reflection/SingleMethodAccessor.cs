using System;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQL.Reflection
{
    internal class SingleMethodAccessor : IAccessor
    {
        private MethodInfo _getter;

        public SingleMethodAccessor(MethodInfo getter)
        {
            _getter = getter;
        }

        public string FieldName => _getter.Name;
        public Type ReturnType => _getter.ReturnType;
        public Type DeclaringType => _getter.DeclaringType;
        public ParameterInfo[] Parameters => _getter.GetParameters();
        public MethodInfo MethodInfo => _getter;
        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return _getter.GetCustomAttributes<T>();
        }

        public object GetValue(object target, object[] arguments)
        {
            return _getter.Invoke(target, arguments);
        }
    }
}
