using System.Reflection;

namespace GraphQL
{
    /// <summary>
    /// Does not add the marked property to the auto-registered GraphQL type as a field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class IgnoreAttribute : GraphQLAttribute
    {
        /// <inheritdoc/>
        public override bool ShouldInclude(MemberInfo memberInfo, bool? isInputType) => false;
    }
}
