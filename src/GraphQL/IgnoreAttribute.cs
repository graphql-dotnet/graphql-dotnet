using System;
using System.Reflection;

namespace GraphQL
{
    /// <summary>
    /// Does not add the marked property to the auto-registered GraphQL type as a field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IgnoreAttribute : GraphQLAttribute
    {
        public override bool ShouldInclude(MemberInfo memberInfo, bool isInputType) => false;
    }
}
