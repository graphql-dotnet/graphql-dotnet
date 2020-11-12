using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GraphQL.Reflection
{
    internal class SingleMethodAccessor : IAccessor
    {
        public SingleMethodAccessor(MethodInfo method)
        {
            MethodInfo = method;
        }

        public string FieldName => MethodInfo.Name;
        public Type ReturnType => MethodInfo.ReturnType;
        public Type DeclaringType => MethodInfo.DeclaringType;
        public ParameterInfo[] Parameters => MethodInfo.GetParameters();
        public MethodInfo MethodInfo { get; }
        public IEnumerable<T> GetAttributes<T>() where T : Attribute => MethodInfo.GetCustomAttributes<T>();

        public object GetValue(object target, object[] arguments)
        {
            try
            {
                return MethodInfo.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return null; // never executed, necessary only for intellisense
            }
        }
    }
}
