using System;
using System.Collections.Generic;
using System.Reflection;

namespace GraphQL.Reflection
{
    internal class SingleMethodAccessor : IAccessor
    {
        public SingleMethodAccessor(MethodInfo getter)
        {
            MethodInfo = getter;
        }

        public string FieldName => MethodInfo.Name;
        public Type ReturnType => MethodInfo.ReturnType;
        public Type DeclaringType => MethodInfo.DeclaringType;
        public ParameterInfo[] Parameters => MethodInfo.GetParameters();
        public MethodInfo MethodInfo { get; }
        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return MethodInfo.GetCustomAttributes<T>();
        }

        public object GetValue(object target, object[] arguments)
        {
            return MethodInfo.Invoke(target, arguments);
        }
    }
}
